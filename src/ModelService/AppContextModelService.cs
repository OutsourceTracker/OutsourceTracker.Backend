using OutsourceTracker.Data;

namespace OutsourceTracker.ModelService;

public abstract class AppContextModelService<TModel, TCreateRequest, TFindRequest, TDeleteRequest, TUpdateRequest> : IModelService<Guid, TModel, TFindRequest>, IWritableModelService<Guid, TModel, TCreateRequest, TUpdateRequest, TDeleteRequest> where TModel : IServiceModel<Guid>
{
    protected AppDataContext DataSource { get; }

    protected ILogger Logger { get; }

    protected virtual string ModelName { get; }

    protected AppContextModelService(AppDataContext context, ILogger logger)
    {
        DataSource = context;
        Logger = logger;
        ModelName = typeof(TModel).Name;
    }


    public abstract Task<TModel?> Get(Guid id, CancellationToken cancellationToken = default);

    public abstract Task<Guid> Create(TCreateRequest? request, CancellationToken cancellationToken = default);

    public abstract ValueTask<bool> Delete(Guid id, TDeleteRequest? request, CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<TModel> Find(TFindRequest? request = default, CancellationToken cancellationToken = default);

    public abstract Task<TModel?> Update(Guid id, TUpdateRequest? request, CancellationToken cancellationToken = default);
}
