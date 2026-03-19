using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Models.Trailers;

namespace OutsourceTracker.Services.ModelService;

internal sealed class TrailerDataService : EquipmentDataService<Trailer>
{
    public TrailerDataService(IServiceProvider services) : base(services)
    {
    }

    protected override Task NormalizeModel(Trailer model, CancellationToken cancellationToken)
    {
        model.Prefix = model.Prefix.ToUpperInvariant();
        model.Name = model.Name.ToUpperInvariant();
        model.FullName = model.Prefix + model.Name;
        return base.NormalizeModel(model, cancellationToken);
    }

    protected override Task OnModelCreated(Trailer model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        model.Prefix ??= "NEW";
        model.Type = TrailerType.Van;
        return base.OnModelCreated(model, cancellationToken);
    }
}
