using OutsourceTracker.Data;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Models.Trailers;

namespace OutsourceTracker.Services.ModelService;

public class TrailerDataService : EquipmentDataService<Trailer>
{
    public TrailerDataService(AppDataContext context, ILogger<TrailerDataService> logger) : base(context, logger)
    {
    }

    protected override Task OnApplyDefaultValues(Trailer newModel, CancellationToken cancellationToken)
    {
        newModel.Prefix = "JBHZ";
        newModel.Name = newModel.Id.ToString().ToUpperInvariant().Substring(0, 6);
        newModel.FullName = newModel.Prefix + newModel.Name;
        newModel.CreatedOn = DateTime.Now;
        newModel.State = EquipmentState.Available;
        newModel.Type = TrailerType.Van;
        return base.OnApplyDefaultValues(newModel, cancellationToken);
    }

    protected override Task NormalizeModel(Trailer model, CancellationToken cancellationToken)
    {
        model.Prefix = model.Prefix.ToUpperInvariant();
        model.Name = model.Name.ToUpperInvariant();
        model.FullName = model.Prefix + model.Name;
        return base.NormalizeModel(model, cancellationToken);
    }
}
