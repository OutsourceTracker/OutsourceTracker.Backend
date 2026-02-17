using OutsourceTracker.Data;
using OutsourceTracker.Equipment;
using System.Reflection;

namespace OutsourceTracker.Services.ModelService;

internal abstract class EquipmentDataService<TModel> : DynamicDataService<TModel> where TModel : class, IEquipment<Guid>
{
    protected EquipmentDataService(AppDataContext context, ILogger logger) : base(context, logger)
    {
    }
}
