using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Srm.Gateway.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace Srm.Gateway.Infrastructure.Interceptors;

public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        // Identify the user making the change
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? currentUserId = null;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedGuid))
        {
            currentUserId = parsedGuid;
        }

        // Track Added, Modified, or Deleted entities (excluding logs)
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            // Extract Primary Key
            var pk = entry.Metadata.FindPrimaryKey();
            var entityId = pk != null
                ? string.Join("-", pk.Properties.Select(p => entry.Property(p.Name).CurrentValue))
                : "Unknown";

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State.ToString(),
                CreatedAt = DateTime.UtcNow,
                UserId = currentUserId,
                Changes = SerializeChanges(entry)
            };

            context.Set<AuditLog>().Add(auditLog);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private string SerializeChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        if (entry.State == EntityState.Added)
        {
            foreach (var prop in entry.CurrentValues.Properties)
            {
                changes[prop.Name] = entry.CurrentValues[prop];
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            foreach (var prop in entry.OriginalValues.Properties)
            {
                var original = entry.OriginalValues[prop];
                var current = entry.CurrentValues[prop];

                if (!Equals(original, current))
                {
                    changes[prop.Name] = new { From = original, To = current };
                }
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            foreach (var prop in entry.OriginalValues.Properties)
            {
                changes[prop.Name] = entry.OriginalValues[prop];
            }
        }

        return JsonSerializer.Serialize(changes, options);
    }
}