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
            if(context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                    (e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted));

            foreach(var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entry.Property("Id").CurrentValue?.ToString() ?? "Unknow",
                    Action = entry.State.ToString(),
                    UserId = Guid.Empty,
                    Changes = GetChanges(entry)
                };

                context.Set<AuditLog>().Add(auditLog);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private string GetChanges(EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();

            if(entry.State == EntityState.Added)
            {
                foreach(var prop in entry.CurrentValues.Properties)
                {
                    if (prop.Name is "Id" or "CreatedAt" or "UpdatedAt") continue;
                    changes[prop.Name] = entry.CurrentValues[prop.Name];
                }
            }
            else if(entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.CurrentValues.Properties)
                {
                    if (prop.Name is "Id" or "CreatedAt" or "UpdatedAt") continue;

                    var original = entry.OriginalValues[prop.Name];
                    var current = entry.CurrentValues[prop.Name];

                    if(!Equals(original, current))
                    {
                        changes[prop.Name] = new { Old = original, New = current };
                    }
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // CRITIQUE : Pour une suppression, on sauvegarde TOUTES les valeurs originales
                // afin de pouvoir potentiellement reconstruire l'objet ou prouver ce qui a été supprimé.
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    if (prop.Name is "Id" or "CreatedAt" or "UpdatedAt") continue;
                    changes[prop.Name] = entry.OriginalValues[prop.Name];
                }
            }

            return JsonSerializer.Serialize(changes);
        }
    }
}
