using OutsourceTracker.Data;
using OutsourceTracker.Equipment.Trailers;

namespace OutsourceTracker.Services.ModelService;

public abstract class EquipmentDataService<TModel> : DynamicDataService<TModel> where TModel : class, ITrailer<Guid>, new()
{
    protected EquipmentDataService(AppDataContext context, ILogger logger) : base(context, logger)
    {
    }
}
