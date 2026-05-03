using Srm.Gateway.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IN8nService
    {
        Task<IEnumerable<DocumentEscalationDto>> GetPendingEscalationsAsync();

        Task<bool> IncrementLevelAsync(Guid documentId);
    }
}
