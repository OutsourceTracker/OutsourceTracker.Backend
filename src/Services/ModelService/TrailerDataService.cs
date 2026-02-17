using OutsourceTracker.Data;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Models.Trailers;

namespace OutsourceTracker.Services.ModelService;

internal sealed class TrailerDataService : EquipmentDataService<Trailer>
{
    public TrailerDataService(AppDataContext context, ILogger<TrailerDataService> logger) : base(context, logger)
    {
    }

    protected override Task NormalizeModel(Trailer model, CancellationToken cancellationToken)
    {
        model.Prefix = model.Prefix.ToUpperInvariant();
        model.Name = model.Name.ToUpperInvariant();
        model.FullName = model.Prefix + model.Name;
        return base.NormalizeModel(model, cancellationToken);
    }

    protected override Trailer InstantiateModel()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.CreateVersion7(now);
        Trailer model = new()
        {
            Id = id,
            CreatedOn = now,
            Prefix = "NEW",
            Type = TrailerType.Van,
            State = EquipmentState.Available
        };

        model.Name = new string(model.Id.ToString("N").TakeLast(6).ToArray());
        return model;
    }
}
