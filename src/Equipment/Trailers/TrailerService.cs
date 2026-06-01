using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Equipment.Trailers;

internal class TrailerService : AppDataModelService<TrailerDbModel>
{
    public TrailerService(IServiceProvider services) : base(services)
    {
    }

    protected override ValueTask OnModelCreated(TrailerDbModel model, CancellationToken cancellationToken = default)
    {
        model.Prefix = model.Prefix.Trim().ToUpperInvariant();
        model.Name = model.Name.Trim().ToUpperInvariant();
        model.FullName = $"{model.Prefix} {model.Name}";
        model.CreatedOn = DateTimeOffset.UtcNow;
        model.Id = Guid.CreateVersion7(model.CreatedOn);
        return base.OnModelCreated(model, cancellationToken);
    }


    /// <summary>
    /// Creates multiple trailers in a single database transaction.
    /// Duplicates within the batch or against existing trailers are reported as failures.
    /// </summary>
    public async Task<ModelResult> BulkCreate(IEnumerable<TrailerCreateRequest> requests, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new BulkCreateResult<TrailerModel>();
        var candidates = new List<TrailerDbModel>();
        var seenFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int index = 0;
        foreach (var req in requests ?? Enumerable.Empty<TrailerCreateRequest>())
        {
            index++;

            if (req == null)
            {
                result.Failed[$"Item #{index}"] = "Request was null.";
                continue;
            }

            if (string.IsNullOrWhiteSpace(req.Prefix) || string.IsNullOrWhiteSpace(req.Name))
            {
                string key = !string.IsNullOrWhiteSpace(req.Prefix) ? req.Prefix :
                             !string.IsNullOrWhiteSpace(req.Name) ? req.Name : $"Item #{index}";
                result.Failed[key] = "Prefix and Name are required.";
                continue;
            }

            var model = new TrailerDbModel();

            // Apply incoming values
            model.ApplyObjectToModel(req);

            // Apply the same normalization as single create path
            model.Prefix = model.Prefix.Trim().ToUpperInvariant();
            model.Name = model.Name.Trim().ToUpperInvariant();
            model.FullName = $"{model.Prefix} {model.Name}";
            model.CreatedOn = DateTimeOffset.UtcNow;
            model.Id = Guid.CreateVersion7(model.CreatedOn);

            string identifier = model.FullName;

            if (!seenFullNames.Add(identifier))
            {
                result.Failed[identifier] = "Duplicate within the submitted list.";
                continue;
            }

            candidates.Add(model);
        }

        if (candidates.Count == 0)
        {
            return ModelResult.Builder()
                .WithResult(result)
                .WithSuccess() // success with zero created is still "processed"
                .Build();
        }

        using var transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SelectedTable.AddRangeAsync(candidates, cancellationToken);
            await DataContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // All candidates succeeded
            result.Created.AddRange(candidates.Cast<TrailerModel>());
            Logger.LogInformation("Bulk created {Count} trailers.", candidates.Count);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_Trailer_FullName_Unique", StringComparison.OrdinalIgnoreCase) == true ||
                                            ex.Message.Contains("IX_Trailer_FullName_Unique", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogWarning(ex, "Bulk trailer create failed due to duplicate FullName (unique constraint).");

            // We have to fall back to one-by-one to identify exactly which ones conflicted.
            // This is rare (only when race condition with other users or the in-batch check missed something).
            return await BulkCreateFallbackOneByOne(requests!, result, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Bulk trailer create failed with unexpected error.");

            // Report remaining candidates as failed
            foreach (var c in candidates)
            {
                string key = c.FullName;
                if (!result.Failed.ContainsKey(key))
                {
                    result.Failed[key] = ex.Message;
                }
            }

            return ModelResult.Builder()
                .WithResult(result)
                .AddError("BulkCreateError", ex.Message)
                .Build();
        }

        return ModelResult.Builder()
            .WithResult(result)
            .WithSuccess()
            .Build();
    }

    /// <summary>
    /// Safely converts a value (which may be a Guid, string, JsonElement, or null) into a Guid?.
    /// Used because JSON deserialization often turns Guids into strings or JsonElement.
    /// </summary>
    private static Guid? TryParseGuid(object? value)
    {
        if (value == null)
            return null;

        if (value is Guid guid)
            return guid;

        if (value is string str && Guid.TryParse(str, out var parsed))
            return parsed;

        // Handle JsonElement (what we actually receive from System.Text.Json)
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                string? strValue = jsonElement.GetString();
                if (strValue != null && Guid.TryParse(strValue, out var parsedFromJson))
                    return parsedFromJson;
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return null;
            }
        }

        return null;
    }

    private async Task<ModelResult> BulkCreateFallbackOneByOne(
        IEnumerable<TrailerCreateRequest> requests,
        BulkCreateResult<TrailerModel> partialResult,
        CancellationToken cancellationToken)
    {
        // Reset created list since we rolled back
        partialResult.Created.Clear();

        foreach (var req in requests)
        {
            if (req == null) continue;

            string identifier = $"{req.Prefix?.Trim().ToUpperInvariant() ?? ""} {req.Name?.Trim().ToUpperInvariant() ?? ""}".Trim();

            try
            {
                // Reuse the single-item Create path (it has its own transaction + normalization)
                ModelResult single = await Create(req, cancellationToken);

                if (single.Success && single.Data is TrailerModel created)
                {
                    // Avoid adding duplicates if somehow already present
                    if (!partialResult.Created.Any(t => t.Id == created.Id))
                    {
                        partialResult.Created.Add(created);
                    }
                }
                else if (single.Errors != null && single.Errors.Count > 0)
                {
                    string err = string.Join("; ", single.Errors.Select(kv => $"{kv.Key}: {kv.Value}"));
                    if (!partialResult.Failed.ContainsKey(identifier))
                    {
                        partialResult.Failed[identifier] = err;
                    }
                }
                else
                {
                    if (!partialResult.Failed.ContainsKey(identifier))
                    {
                        partialResult.Failed[identifier] = "Unknown error during creation.";
                    }
                }
            }
            catch (Exception ex)
            {
                if (!partialResult.Failed.ContainsKey(identifier))
                {
                    partialResult.Failed[identifier] = ex.Message;
                }
            }
        }

        return ModelResult.Builder()
            .WithResult(partialResult)
            .WithSuccess() // We processed everything; some may have failed
            .Build();
    }

    /// <summary>
    /// Deletes multiple trailers in a single database transaction.
    /// </summary>
    public async Task<ModelResult> BulkDelete(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new BulkDeleteResult();
        var idsList = ids?.Distinct().ToList() ?? [];

        if (idsList.Count == 0)
        {
            return ModelResult.Builder().WithResult(result).WithSuccess().Build();
        }

        // Load existing trailers
        var existing = await SelectedTable
            .Where(t => idsList.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var existingIds = existing.Select(e => e.Id).ToHashSet();

        foreach (var id in idsList)
        {
            if (!existingIds.Contains(id))
            {
                result.Failed[id] = "Trailer not found.";
            }
        }

        if (existing.Count == 0)
        {
            return ModelResult.Builder().WithResult(result).WithSuccess().Build();
        }

        using var transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            SelectedTable.RemoveRange(existing);
            int affected = await DataContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.SuccessfulIds = existing.Select(e => e.Id).ToArray();
            Logger.LogInformation("Bulk deleted {Count} trailers.", existing.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Bulk trailer delete failed.");

            // Mark all attempted existing ones as failed
            foreach (var model in existing)
            {
                result.Failed[model.Id] = ex.Message;
            }
            result.SuccessfulIds = [];

            return ModelResult.Builder()
                .WithResult(result)
                .AddError("BulkDeleteError", ex.Message)
                .Build();
        }

        return ModelResult.Builder()
            .WithResult(result)
            .WithSuccess()
            .Build();
    }

    /// <summary>
    /// Applies the same set of changes to multiple trailers in a single transaction.
    /// </summary>
    public async Task<ModelResult> BulkUpdate(IEnumerable<Guid> ids, object updateValues, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new BulkUpdateResult<TrailerModel>();
        var idsList = ids?.Distinct().ToList() ?? [];

        if (idsList.Count == 0)
        {
            return ModelResult.Builder().WithResult(result).WithSuccess().Build();
        }

        if (updateValues == null)
        {
            return ModelResult.Builder()
                .WithResult(result)
                .AddError("NoChanges", "No update values provided.")
                .Build();
        }

        // Load trailers
        var trailers = await SelectedTable
            .Where(t => idsList.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (trailers.Count == 0)
        {
            foreach (var id in idsList)
            {
                result.Failed[id] = "Trailer not found.";
            }
            return ModelResult.Builder().WithResult(result).WithSuccess().Build();
        }

        var foundIds = trailers.Select(t => t.Id).ToHashSet();
        foreach (var id in idsList.Where(id => !foundIds.Contains(id)))
        {
            result.Failed[id] = "Trailer not found.";
        }

        using var transaction = await DataContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            bool anyChanged = false;

            foreach (var trailer in trailers)
            {
                try
                {
                    // Defensive FK validation to prevent FOREIGN KEY constraint failures
                    // when clients send stale or invalid AccountId / ZoneId values.
                    // Note: Values often arrive as strings from JSON, not Guids.
                    Guid? newAccountId = null;
                    if (updateValues is IDictionary<string, object> updateDict)
                    {
                        newAccountId = TryParseGuid(updateDict.ContainsKey("AccountId") ? updateDict["AccountId"] : null);

                        if (newAccountId.HasValue && newAccountId.Value != Guid.Empty)
                        {
                            bool accountExists = await DataContext.BusinessAccounts
                                .AnyAsync(a => a.Id == newAccountId.Value, cancellationToken);
                            if (!accountExists)
                            {
                                result.Failed[trailer.Id] = "Invalid AccountId: the referenced account does not exist.";
                                continue; // Skip this trailer
                            }
                        }
                    }

                    bool changed = trailer.ApplyObjectToModel(updateValues);

                    if (changed)
                    {
                        // Re-normalize if Prefix or Name were touched (mirrors create logic)
                        if (updateValues is IDictionary<string, object> dict)
                        {
                            var keys = dict.Keys.Select(k => k.ToLowerInvariant()).ToHashSet();
                            if (keys.Contains("prefix") || keys.Contains("name"))
                            {
                                trailer.Prefix = (trailer.Prefix?.Trim().ToUpperInvariant()) ?? string.Empty;
                                trailer.Name = (trailer.Name?.Trim().ToUpperInvariant()) ?? string.Empty;
                                trailer.FullName = $"{trailer.Prefix} {trailer.Name}".Trim();
                            }
                        }

                        SelectedTable.Update(trailer);
                        anyChanged = true;
                    }

                    result.Updated.Add(trailer);
                }
                catch (Exception itemEx)
                {
                    result.Failed[trailer.Id] = itemEx.Message;
                }
            }

            if (anyChanged)
            {
                await DataContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            Logger.LogInformation("Bulk updated {Count} trailers.", result.Updated.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Bulk trailer update failed.");

            result.Updated.Clear();
            foreach (var id in foundIds)
            {
                if (!result.Failed.ContainsKey(id))
                    result.Failed[id] = ex.Message;
            }

            return ModelResult.Builder()
                .WithResult(result)
                .AddError("BulkUpdateError", ex.Message)
                .Build();
        }

        return ModelResult.Builder()
            .WithResult(result)
            .WithSuccess()
            .Build();
    }
}
