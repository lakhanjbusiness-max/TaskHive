using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskHive.Application.Common;
using TaskHive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace TaskHive.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/diagnostics")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITenantContext _tenantContext;

        public DiagnosticsController(AppDbContext db, ITenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        [HttpGet("db")]
        public async Task<IActionResult> Db(CancellationToken cancellationToken)
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            var tenantCount = await _db.Tenants.CountAsync(cancellationToken);
            var userCount = await _db.Users.CountAsync(cancellationToken);

            return Ok(new
            {
                canConnect,
                tenantCount,
                userCount,
                currentTenantId = _tenantContext.TenantId
            });
        }
    }
}