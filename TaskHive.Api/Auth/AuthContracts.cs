namespace TaskHive.Api.Auth
{
    public record SignupRequest(string TenantName, string Email, string Password);

    public record LoginRequest(string TenantName, string Email, string Password);

    public record AuthResponse(string AccessToken, Guid TenantId, string Email, string Role);
}
