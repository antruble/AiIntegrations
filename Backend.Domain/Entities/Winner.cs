using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Domain.Entities
{
    public class Winner
    {
        public Guid HandId { get; set; }
        public Guid PlayerId { get; set; }
        public required Player Player { get; set; }
        public int Pot { get; set; } = 0;

    }
}
