using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Domain.Entities
{
    public class DocumentFieldValue
    {
        public object Value { get; set; } = null!;
        public double Confidence { get; set; }
    }
}
