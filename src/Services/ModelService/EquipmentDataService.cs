using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Equipment;

namespace OutsourceTracker.Services.ModelService;

internal abstract class EquipmentDataService<TModel> : DynamicDataService<TModel> where TModel : class, IEquipment<Guid>
{
    protected EquipmentDataService(IServiceProvider services) : base(services)
    {
    }

    protected override async ValueTask<bool> ApplyModelUpdate(TModel model, object updateValues, CancellationToken cancellationToken)
    {
        bool updated = await base.ApplyModelUpdate(model, updateValues, cancellationToken);

        if (updated)
        {
            if (!model.Location.HasValue)
            {
                model.ZoneId = null;
                model.ZoneName = null;
                model.LocatedBy = null;
                model.LocatedDate = null;
            }
            else
            {
                var zone = await DataContext.Zones
                    .AsNoTracking()
                    .ToListAsync(cancellationToken)
                    .ContinueWith(t => t.Result
                    .FirstOrDefault(z => z.Boundry.Contains(model.Location.Value)), cancellationToken);

                if (zone != null)
                {
                    model.ZoneId = zone.Id;
                    model.ZoneName = zone.Name;
                }
                else
                {
                    model.ZoneId = null;
                    model.ZoneName = null;
                }
            }
        }

        return updated;
    }

    protected override Task OnModelCreated(TModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        model.State = EquipmentState.Available;
        model.Name ??= new string(model.Id.ToString("N").TakeLast(6).ToArray());
        return base.OnModelCreated(model, cancellationToken);
    }
}
