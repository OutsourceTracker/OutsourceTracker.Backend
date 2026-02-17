using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services.ModelService;

internal abstract class DynamicDataService<TModel> : IModelCreateService<TModel>, IModelDeleteService<TModel>, IModelUpdateService<TModel>, IModelLookupService<TModel> where TModel : class, IServiceModel<Guid>
{
    protected AppDataContext DataContext { get; }

    protected ILogger Logger { get; }

    protected virtual string ModelName { get; } = typeof(TModel).Name;

    protected virtual DbSet<TModel> SelectedTable { get; }

    protected DynamicDataService(AppDataContext context, ILogger logger)
    {
        DataContext = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dbSetProperty = typeof(AppDataContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(DbSet<TModel>));

        if (dbSetProperty == null)
        {
            throw new InvalidOperationException($"No DbSet<{ModelName}> found in {nameof(AppDataContext)}.");
        }

        Logger.LogDebug("Selected DbSet for {ModelName}: {TableName}", ModelName, dbSetProperty.Name);
        SelectedTable = (DbSet<TModel>)dbSetProperty.GetValue(context)!;
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

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing DELETE for {ModelName} with ID {ModelId}", ModelName, id);
        TModel? model = await FindModelById(id, cancellationToken);

        if (model == null)
        {
            Logger.LogWarning("No {ModelName} found with ID {ModelId} for deletion", ModelName, id);
            return false;
        }

        await OnModelFound(model, nameof(Delete), cancellationToken);
        await OnRemoveModel(model, cancellationToken);
        int affected = await OnWriteDatabase(model, cancellationToken);

        if (affected > 0)
        {
            Logger.LogInformation("Deleted {ModelName} with ID {ModelId}. Affected rows: {AffectedRows}", ModelName, id, affected);
            return true;
        }

        Logger.LogDebug("DELETE executed for {ModelName} with ID {ModelId}, but no rows affected", ModelName, id);
        return false;
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

    public async Task<TModel?> Update(Guid id, object request, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing UPDATE for {ModelName} with ID {ModelId} and request {Request}", ModelName, id, request);

        if (request == null)
        {
            Logger.LogWarning("Skipping UPDATE for {ModelName} with ID {ModelId}: Request parameters are empty", ModelName, id);
            return null;
        }

        TModel? model = await SelectedTable.FindAsync([id], cancellationToken);

        if (model == null)
        {
            Logger.LogWarning("No {ModelName} found with ID {ModelId} for update", ModelName, id);
            return null;
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
        return null;
    }

    public async Task<Guid?> Create(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing CREATE for {ModelName}", ModelName);
        TModel model = InstantiateModel();
        await NormalizeModel(model, cancellationToken);
        await SelectedTable.AddAsync(model, cancellationToken);
        int affected = await OnWriteDatabase(model, cancellationToken);

        if (affected > 0)
        {
            Logger.LogInformation("Created new {ModelName} with ID {ModelId}. Affected rows: {AffectedRows}", ModelName, model.Id, affected);
            return model.Id;
        }

        Logger.LogError("CREATE executed for {ModelName}, but no rows affected", ModelName);
        throw new InvalidOperationException($"Failed to create {ModelName}: No rows affected.");
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

    protected abstract TModel InstantiateModel();

    #endregion
}