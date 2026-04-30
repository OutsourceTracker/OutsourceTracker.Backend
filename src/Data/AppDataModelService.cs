using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;
using System.Reflection;

namespace OutsourceTracker.Data
{
    internal abstract class AppDataModelService<TModel> : IDataModelService<Guid, TModel> where TModel : class, new()
    {
        protected AppDataContext DataContext { get; }

        protected ILogger Logger { get; }

        protected IServiceProvider Services { get; }

        public string ModelName { get; protected set; } = typeof(TModel).Name;

        protected DbSet<TModel> SelectedTable { get; }

        protected AppDataModelService(IServiceProvider services)
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
        }


        public async Task<ModelResult> Create<T>(T? modelParameters = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TModel? model = null;

            if (modelParameters is TModel typedModel)
            {
                Logger.LogDebug("Model parameters are already of type {ModelName}. Using directly.", ModelName);
                model = typedModel;
            }
            else
            {
                Logger.LogDebug("Creating new instance of {ModelName}.", ModelName);
                model = new TModel();
                
                if (modelParameters != null)
                {
                    Logger.LogDebug("Applying model parameters to new {ModelName} instance.", ModelName);
                    bool applied = model.ApplyObjectToModel(modelParameters);
                    Logger.LogDebug("Model parameters applied: {Applied}", applied);
                }
            }

            using IModelResultBuilder r = ModelResult.Builder();
            Logger.LogDebug("Beginning transaction for creating {ModelName}.", ModelName);
            IDbContextTransaction transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                Logger.LogDebug("Adding {ModelName} to DbSet.", ModelName);
                await SelectedTable.AddAsync(model, cancellationToken);
                Logger.LogDebug("Added {ModelName} to DbSet. Saving changes to database.", ModelName);
                await DataContext.SaveChangesAsync(cancellationToken);
                Logger.LogDebug("Changes saved to database. Committing transaction for {ModelName}.", ModelName);
                await transaction.CommitAsync(cancellationToken);
                Logger.LogInformation("{ModelName} created | {Model}", ModelName, model);
                r.WithResult(model)
                    .WithSuccess();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding {ModelName} to database.", ModelName);
                r.AddError(ex.GetType().Name, ex);
                await transaction.RollbackAsync(cancellationToken);
                Logger.LogDebug("Transaction rolled back for {ModelName} creation.", ModelName);
            }
            finally
            {
                Logger.LogDebug("Disposing transaction for {ModelName} creation.", ModelName);
                await transaction.DisposeAsync();
                Logger.LogDebug("Transaction disposed for {ModelName} creation.", ModelName);
            }

            return r.Build();
        }

        public async Task<ModelResult> Delete(Guid modelId, CancellationToken cancellationToken = default)
        {
            ModelResult result = await Get(modelId, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            TModel model = (TModel)result.Data!;
            using IModelResultBuilder r = ModelResult.Builder();
            IDbContextTransaction transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                Logger.LogDebug("Removing {ModelName} with ID {ModelId} from DbSet.", ModelName, modelId);
                SelectedTable.Remove(model);
                await DataContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("{ModelName} deleted | {Model}", ModelName, model);
                await transaction.CommitAsync(cancellationToken);
                Logger.LogDebug("Transaction committed for {ModelName} deletion.", ModelName);
                return r.WithSuccess().Build();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting {ModelName} with ID {ModelId}.", ModelName, modelId);
                r.AddError(ex.GetType().Name, ex);
                await transaction.RollbackAsync(cancellationToken);
                Logger.LogDebug("Transaction rolled back for {ModelName} deletion.", ModelName);
            }
            finally
            {
                Logger.LogDebug("Disposing transaction for {ModelName} deletion.", ModelName);
                await transaction.DisposeAsync();
                Logger.LogDebug("Transaction disposed for {ModelName} deletion.", ModelName);
            }

            return r.Build();
        }

        public async Task<ModelResult> Get(Guid modelId, CancellationToken cancellationToken = default)
        {
            using IModelResultBuilder r = ModelResult.Builder();
            TModel? model = await SelectedTable.FindAsync([modelId], cancellationToken);

            if (model != null)
            {
                Logger.LogInformation("{ModelName} retrieved | {Model}", ModelName, model);
                return r.WithResult(model)
                        .WithSuccess()
                        .Build();
            }

            Logger.LogWarning("{ModelName} not found | {ModelId}", ModelName, modelId);
            return r.AddError("NotFound", modelId)
                    .Build();
        }

        public async Task<ModelResult> Update<T>(Guid modelId, T modelParameters, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(modelParameters, nameof(modelParameters));
            ModelResult result = await Get(modelId, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            TModel model = (TModel)result.Data!;
            using IModelResultBuilder r = ModelResult.Builder();
            IDbContextTransaction transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                Logger.LogDebug("Applying update parameters to {ModelName} with ID {ModelId}.", ModelName, modelId);
                bool applied = model.ApplyObjectToModel(modelParameters);
                Logger.LogDebug("Update parameters applied: {Applied}", applied);
                if (!applied)
                {
                    Logger.LogWarning("No update parameters were applied to {ModelName} with ID {ModelId}.", ModelName, modelId);
                    return r.AddError("NoChanges", "No valid update parameters provided.")
                            .Build();
                }
                await DataContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("{ModelName} updated | {Model}", ModelName, model);
                await transaction.CommitAsync(cancellationToken);
                Logger.LogDebug("Transaction committed for {ModelName} update.", ModelName);
                return r.WithResult(model)
                        .WithSuccess()
                        .Build();

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating {ModelName} with ID {ModelId}.", ModelName, modelId);
                r.AddError(ex.GetType().Name, ex);
                await transaction.RollbackAsync(cancellationToken);
                Logger.LogDebug("Transaction rolled back for {ModelName} update.", ModelName);
            }
            finally
            {
                Logger.LogDebug("Disposing transaction for {ModelName} update.", ModelName);
                await transaction.DisposeAsync();
                Logger.LogDebug("Transaction disposed for {ModelName} update.", ModelName);
            }

            return r.Build();
        }

        public async Task<ModelResult> Search<T>(T? searchParameters = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        
    }
}
