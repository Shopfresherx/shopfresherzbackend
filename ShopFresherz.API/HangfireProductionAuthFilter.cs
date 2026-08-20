using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace ShopFresherz.API;

/// <summary>Restricts Hangfire dashboard access in production to Admin/SuperAdmin only.</summary>
public sealed class HangfireProductionAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        HttpContext http = context.GetHttpContext();
        // Only allow authenticated users with Admin or SuperAdmin role.
        return http.User.Identity?.IsAuthenticated == true &&
               (http.User.IsInRole("Admin") || http.User.IsInRole("SuperAdmin"));
    }
}