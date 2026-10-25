using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHive.Domain.Common;

namespace TaskHive.Domain.Entities
{
    public class Project : BaseEntity, ITenantOwned
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
    }
}
