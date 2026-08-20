using Hangfire.Dashboard;

namespace ShopFresherz.API;

/// <summary>Allows Hangfire dashboard access in development wiring.</summary>
public sealed class HangfireDevAuthFilter : IDashboardAuthorizationFilter
{
    /// <inheritdoc />
    public bool Authorize(DashboardContext context) => true;
}
