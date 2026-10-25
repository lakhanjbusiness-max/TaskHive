using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHive.Domain.Common;

namespace TaskHive.Domain.Entities
{
    public class User: BaseEntity, ITenantOwned
    {
        public Guid TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = "Member";  // Admin | Member
    }
}
