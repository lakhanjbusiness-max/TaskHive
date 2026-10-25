using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHive.Domain.Common;

namespace TaskHive.Domain.Entities
{
    public class TaskItem : BaseEntity, ITenantOwned
    {                            // "Task" collides with System.Threading.Tasks.Task — name it TaskItem
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Todo";   // Todo | InProgress | Done
        public string Priority { get; set; } = "Medium";
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
