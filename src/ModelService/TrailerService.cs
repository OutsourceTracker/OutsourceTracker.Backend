using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.ModelService.Requests;
using OutsourceTracker.ModelService.Requests.Trailers;

namespace OutsourceTracker.ModelService;

public class TrailerService : AppContextModelService<CommercialTrailer, TrailerCreateRequest, TrailerFindRequest, DeleteRequest, TrailerUpdateRequest>
{
    public TrailerService(AppDataContext context, ILogger<TrailerService> logger) : base(context, logger)
    {
    }

    public override async Task<Guid> Create(TrailerCreateRequest? request, CancellationToken cancellationToken = default)
    {
        CommercialTrailer model = new();

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Prefix))
            {
                model.Prefix = request.Prefix.Trim().ToUpperInvariant();
                Logger.LogDebug("{MODEL} Prefix set to {PREFIX}", ModelName, request.Prefix);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                model.Name = request.Name.Trim().ToUpperInvariant();
                Logger.LogDebug("{MODEL} Name set to {NAME}", ModelName, request.Name);
            }

            model.FullName = model.Prefix + model.Name;
        }

        await DataSource.Trailers.AddAsync(model, cancellationToken);
        Logger.LogDebug("Now tracking changes for {MODEL} {NAME}", ModelName, model.FullName);
        await DataSource.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Created new {MODEL} {NAME} with ID {ID}", ModelName, model.FullName, model.Id);
        return model.Id;
    }

    public override async ValueTask<bool> Delete(Guid id, DeleteRequest? request, CancellationToken cancellationToken = default)
    {
        CommercialTrailer? model = await Get(id, cancellationToken);

        if (model == null)
        {
            Logger.LogWarning("Could not find {MODEL} with ID {ID} to delete", ModelName, id);
            return false;
        }

        DataSource.Trailers.Remove(model);
        Logger.LogDebug("Now tracking changes for deleted {MODEL} {NAME}", ModelName, model.FullName);
        int changes = await DataSource.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Deleted {MODEL} {NAME} with ID {ID}, changes saved: {CHANGES}", ModelName, model.FullName, model.Id, changes);
        return true;
    }

    public override IAsyncEnumerable<CommercialTrailer> Find(TrailerFindRequest? request = null, CancellationToken cancellationToken = default)
    {
        IQueryable<CommercialTrailer> query = DataSource.Trailers;

        if (request != null)
        {
            if (request.Ids != null && request.Ids.Any())
            {
                query = query.Where(t => request.Ids.Contains(t.Id));
                Logger.LogDebug("Filtering {MODEL} by provided IDs", ModelName);
            }

            if (!string.IsNullOrWhiteSpace(request.Prefix))
            {
                string prefixFilter = request.Prefix.Trim().ToUpperInvariant();
                query = query.Where(t => t.Prefix.Contains(prefixFilter));
                Logger.LogDebug("Filtering {MODEL} by Prefix: {PREFIX}", ModelName, prefixFilter);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                string nameFilter = request.Name.Trim().ToUpperInvariant();
                query = query.Where(t => t.Name.Contains(nameFilter));
                Logger.LogDebug("Filtering {MODEL} by Name: {NAME}", ModelName, nameFilter);
            }

            if (!string.IsNullOrWhiteSpace(request.SpottedBy))
            {
                string spottedByFilter = request.SpottedBy.Trim();
                query = query.Where(t => t.SpottedBy != null && t.SpottedBy.Contains(spottedByFilter, StringComparison.OrdinalIgnoreCase));
                Logger.LogDebug("Filtering {MODEL} by SpottedBy: {SPOTTEDBY}", ModelName, spottedByFilter);
            }
        }
        return query
            .OrderBy(t => t.FullName)
            .AsAsyncEnumerable();
    }

    public override async Task<CommercialTrailer?> Get(Guid id, CancellationToken cancellationToken = default) => await DataSource.Trailers.FindAsync([id], cancellationToken);

    public override async Task<CommercialTrailer?> Update(Guid id, TrailerUpdateRequest? request, CancellationToken cancellationToken = default)
    {
        CommercialTrailer? model = await Get(id, cancellationToken);
        
        if (model == null)
        {
            Logger.LogWarning("Could not find {MODEL} with ID {ID} to update", ModelName, id);
            return null;
        }

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Prefix))
            {
                model.Prefix = request.Prefix.Trim().ToUpperInvariant();
                Logger.LogDebug("{MODEL} Prefix updated to {PREFIX}", ModelName, request.Prefix);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                model.Name = request.Name.Trim().ToUpperInvariant();
                Logger.LogDebug("{MODEL} Name updated to {NAME}", ModelName, request.Name);
            }

            model.FullName = model.Prefix + model.Name;

            bool locationUpdated = false;
            if (request.Latitude.HasValue)
            {
                model.SpottedLatitude = request.Latitude;
                Logger.LogDebug("{MODEL} SpottedLatitude updated to {LATITUDE}", ModelName, request.Latitude);
                locationUpdated = true;
            }

            if (request.Longitude.HasValue)
            {
                model.SpottedLongitude = request.Longitude;
                Logger.LogDebug("{MODEL} SpottedLongitude updated to {LONGITUDE}", ModelName, request.Longitude);
                locationUpdated = true;
            }

            if (locationUpdated)
            {
                if (request.Accuracy.HasValue)
                {
                    model.SpottedAccuracy = request.Accuracy;
                    Logger.LogDebug("{MODEL} SpottedAccuracy updated to {ACCURACY}", ModelName, request.Accuracy);
                }
                else
                                {
                    model.SpottedAccuracy = null;
                }

                if (!string.IsNullOrWhiteSpace(request.SpottedBy))
                {
                    model.SpottedBy = request.SpottedBy;
                    Logger.LogDebug("{MODEL} SpottedBy updated to {SPOTTEDBY}", ModelName, request.SpottedBy);
                }
                else
                {
                    model.SpottedBy = null;
                }

                model.SpottedOn = DateTimeOffset.UtcNow;
            }
        }

        DataSource.Trailers.Update(model);
        Logger.LogDebug("Now tracking changes for updated {MODEL} {NAME}", ModelName, model.FullName);
        await DataSource.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Updated {MODEL} {NAME} with ID {ID}", ModelName, model.FullName, model.Id);
        return model;
    }
}
