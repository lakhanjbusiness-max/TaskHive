using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskHive.Api.Auth;
using TaskHive.Application.Common;
using TaskHive.Domain.Entities;
using TaskHive.Infrastructure.Persistence;

namespace TaskHive.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthController(
            AppDbContext db,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup(
            SignupRequest request,
            CancellationToken cancellationToken)
        {
            var nameTaken = await _db.Tenants
                .AnyAsync(t => t.Name == request.TenantName, cancellationToken);

            if (nameTaken)
                return Conflict(new { message = "That organisation name is already registered." });

            // EF assigns the key as soon as the entity starts being tracked,
            // so tenant.Id is populated here - before SaveChanges.
            var tenant = new Tenant { Name = request.TenantName };
            _db.Tenants.Add(tenant);

            var user = new User
            {
                TenantId = tenant.Id,   // explicit: no JWT exists yet, so SaveChanges can't stamp it
                Email = request.Email,
                Role = "Admin"
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            _db.Users.Add(user);

            // One SaveChanges, one transaction: both rows commit or neither does.
            await _db.SaveChangesAsync(cancellationToken);

            var token = _tokenService.CreateToken(user);
            return Ok(new AuthResponse(token, user.TenantId, user.Email, user.Role));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            // Tenant has no query filter, so this needs no escape hatch.
            var tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Name == request.TenantName, cancellationToken);

            if (tenant is null)
                return Unauthorized(new { message = "Invalid credentials." });

            // THE escape hatch. No tenant is resolved yet, so the global filter would
            // hide the very row we are authenticating against. This is the one place
            // bypassing it is correct - and it is scoped to a single query.
            var user = await _db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    u => u.TenantId == tenant.Id && u.Email == request.Email,
                    cancellationToken);

            if (user is null)
                return Unauthorized(new { message = "Invalid credentials." });

            var verification = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (verification == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _tokenService.CreateToken(user);
            return Ok(new AuthResponse(token, user.TenantId, user.Email, user.Role));
        }
    }
}
