using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Srm.Gateway.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace Srm.Gateway.Infrastructure.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            // 🟢 RÉPARATION DU TYPAGE : Extraction et tentative de parsing en Guid
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? currentUserId = null;

            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                currentUserId = parsedGuid;
            }

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            foreach (var entry in entries)
            {
                // ✅ RÉPARATION CRITIQUE : Récupération dynamique et sécurisée de la clé primaire (simple ou composite)
                var pk = entry.Metadata.FindPrimaryKey();
                var entityId = "Unknown";

                if (pk != null)
                {
                    var pkValues = pk.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
                    entityId = string.Join("-", pkValues);
                }

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
            var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

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

            return JsonSerializer.Serialize(changes, options);
        }
    }
}