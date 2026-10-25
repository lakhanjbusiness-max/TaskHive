using TaskHive.Application.Common;

namespace TaskHive.Api.Auth
{
    public class CurrentTenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentTenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? TenantId
        {
            get
            {
                var raw = _httpContextAccessor.HttpContext?
                    .User.FindFirst(TaskHiveClaims.TenantId)?.Value;

                return Guid.TryParse(raw, out var tenantId) ? tenantId : null;
            }
        }
    }
}