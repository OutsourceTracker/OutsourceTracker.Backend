using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services.ModelService;

internal abstract class DynamicDataService<TModel> : IModelCreateService<TModel>, IModelDeleteService<TModel>, IModelUpdateService<TModel>, IModelLookupService<TModel> where TModel : class, IServiceModel<Guid>
{
    protected AppDataContext DataContext { get; }

    protected ILogger Logger { get; }

    protected IServiceProvider Services { get; }

    protected virtual string ModelName { get; } = typeof(TModel).Name;

    protected virtual DbSet<TModel> SelectedTable { get; }

    private readonly Action<TModel, Guid> _setModelId; 
    private readonly Action<TModel, DateTimeOffset> _setModelCreateOn; 

    protected DynamicDataService(IServiceProvider services)
    {
        Services = services;
        DataContext = services.GetRequiredService<AppDataContext>();
        
        ILoggerFactory factory = services.GetRequiredService<ILoggerFactory>();
        Type categoryType = GetType();
        Logger = factory.CreateLogger(categoryType);

        var dbSetProperty = typeof(AppDataContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(DbSet<TModel>));

        if (dbSetProperty == null)
        {
            throw new InvalidOperationException($"No DbSet<{ModelName}> found in {nameof(AppDataContext)}.");
        }

        Logger.LogDebug("Selected DbSet for {ModelName}: {TableName}", ModelName, dbSetProperty.Name);
        SelectedTable = (DbSet<TModel>)dbSetProperty.GetValue(DataContext)!;
        _setModelId = SetProperty<TModel, Guid>("Id");
        _setModelCreateOn = SetProperty<TModel, DateTimeOffset>("CreatedOn"); 
    }

    public async Task<TModel?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing GET for {ModelName} with ID {ModelId}", ModelName, id);
        TModel? model = await FindModelById(id, cancellationToken);

        if (model != null)
        {
            Logger.LogDebug("Found {ModelName} with ID {ModelId}", ModelName, id);
            await OnModelFound(model, nameof(Get), cancellationToken);
            return model;
        }

        Logger.LogWarning("No {ModelName} found with ID {ModelId}", ModelName, id);
        return null;
    }

    public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing DELETE for {ModelName} with ID {ModelId}", ModelName, id);
        TModel? model = await FindModelById(id, cancellationToken);

        if (model == null)
        {
            Logger.LogWarning("No {ModelName} found with ID {ModelId} for deletion", ModelName, id);
            throw new KeyNotFoundException($"No {ModelName} found with ID {id} for deletion");
        }

        await OnModelFound(model, nameof(Delete), cancellationToken);
        await OnRemoveModel(model, cancellationToken);
        int affected = await OnWriteDatabase(model, cancellationToken);

        if (affected > 0)
        {
            Logger.LogInformation("Deleted {ModelName} with ID {ModelId}. Affected rows: {AffectedRows}", ModelName, id, affected);
            return affected;
        }

        Logger.LogDebug("DELETE executed for {ModelName} with ID {ModelId}, but no rows affected", ModelName, id);
        return 0;
    }

    public async IAsyncEnumerable<TModel> Search(object? searchOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing SEARCH for {ModelName} with options {SearchOptions}", ModelName, searchOptions);
        IQueryable<TModel> query = SelectedTable.AsQueryable();
        query = await OnApplyFilter(query, searchOptions, cancellationToken);

        await foreach (var model in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (model != null)
            {
                await OnModelFound(model, nameof(Search), cancellationToken);
                yield return model;
            }
        }
    }

    public async Task<TModel> Update(Guid id, object request, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing UPDATE for {ModelName} with ID {ModelId} and request {Request}", ModelName, id, request);

        if (request == null)
        {
            Logger.LogWarning("Skipping UPDATE for {ModelName} with ID {ModelId}: Request parameters are empty", ModelName, id);
            ArgumentNullException.ThrowIfNull(request, nameof(request));
        }

        TModel? model = await SelectedTable.FindAsync([id], cancellationToken);

        if (model == null)
        {
            Logger.LogWarning("No {ModelName} found with ID {ModelId} for update", ModelName, id);
            throw new KeyNotFoundException($"No {ModelName} found with ID {id} for update");
        }

        await OnModelFound(model, nameof(Update), cancellationToken);
        bool updated = await ApplyModelUpdate(model, request, cancellationToken);

        if (updated)
        {
            await NormalizeModel(model, cancellationToken);
            SelectedTable.Update(model);
            int affected = await OnWriteDatabase(model, cancellationToken);
            Logger.LogInformation("Updated {ModelName} with ID {ModelId}. Affected rows: {AffectedRows}", ModelName, id, affected);
            return model;
        }

        Logger.LogDebug("No updates applied to {ModelName} with ID {ModelId}: No properties changed", ModelName, id);
        return model;
    }

    public async Task<TModel?> Create(TModel? model = null, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing CREATE for {ModelName}", ModelName);
        model ??= InstantiateModel();
        await NormalizeModel(model, cancellationToken);

        PropertyInfo? idProp = model.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

        if (idProp != null)
        {
            idProp.SetValue(model, GenerateNewId());
        }


        await SelectedTable.AddAsync(model, cancellationToken);
        int affected = await OnWriteDatabase(model, cancellationToken);

        if (affected > 0)
        {
            Logger.LogInformation("Created new {ModelName} with ID {ModelId}. Affected rows: {AffectedRows}", ModelName, model.Id, affected);
            return model;
        }

        Logger.LogError("CREATE executed for {ModelName}, but no rows affected", ModelName);
        throw new InvalidOperationException($"Failed to create {ModelName}: No rows affected.");
    }

    protected TModel InstantiateModel(TModel? model = null, CancellationToken cancellationToken = default)
    {
        model ??= ActivatorUtilities.CreateInstance<TModel>(Services);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.CreateVersion7(now);
        
        _setModelId(model, id);
        _setModelCreateOn(model, now);
        OnModelCreated(model, cancellationToken);
        return model;
    }

    private static Action<T, TValue> SetProperty<T, TValue>(string propertyName)
    {
        var param0 = Expression.Parameter(typeof(T), "x");
        var param1 = Expression.Parameter(typeof(TValue), "y");
        var prop = Expression.Property(param0, propertyName);
        var assign = Expression.Assign(prop, param1);

        var lambda = Expression.Lambda<Action<T, TValue>>(assign, param0, param1);
        var setter = lambda.Compile();
        return setter;
    }

    #region Database Actions

    protected virtual async Task<TModel?> FindModelById(Guid id, CancellationToken cancellationToken) => await SelectedTable.FindAsync([id], cancellationToken);

    protected virtual async ValueTask<int> OnWriteDatabase(TModel model, CancellationToken cancellationToken) => await DataContext.SaveChangesAsync(cancellationToken);

    #endregion

    #region Virtual Overrides

    protected virtual Task<IQueryable<TModel>> OnApplyFilter(IQueryable<TModel> query, object? search, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.AsNoTracking().ApplyObjectFilter(search));
    }

    protected virtual ValueTask<bool> ApplyModelUpdate(TModel model, object updateValues, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(model.ApplyObjectToModel(updateValues));
    }

    protected virtual Task OnRemoveModel(TModel model, CancellationToken cancellationToken)
    {
        SelectedTable.Remove(model);
        return Task.CompletedTask;
    }

    protected virtual Task NormalizeModel(TModel model, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnModelFound(TModel model, string method, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnModelCreated(TModel model, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected Guid GenerateNewId() => Guid.CreateVersion7(DateTimeOffset.UtcNow);

    #endregion
}