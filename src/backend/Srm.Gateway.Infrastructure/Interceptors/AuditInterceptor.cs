using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            // 🟢 LA CORRECTION : .ToList() pour éviter "Collection was modified"
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            foreach (var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entry.Property("Id").CurrentValue?.ToString() ?? "Unknown",
                    Action = entry.State.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UserId = Guid.Empty, // À remplacer par l'ID de l'utilisateur plus tard
                    Changes = SerializeChanges(entry)
                };

                context.Set<AuditLog>().Add(auditLog);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private string SerializeChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();

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

            return JsonSerializer.Serialize(changes);
        }
    }
}
