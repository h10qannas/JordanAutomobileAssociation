using JAA.Models;
using Microsoft.AspNetCore.Identity;

namespace JAA.Middleware
{
    public class SuspensionMiddleware
    {
        private readonly RequestDelegate _next;

        public SuspensionMiddleware(RequestDelegate next) => _next = next;

        // ASP.NET Core resolves scoped services from InvokeAsync parameters automatically.
        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                context.User.IsInRole("Customer"))
            {
                var user = await userManager.GetUserAsync(context.User);
                context.Items["IsSuspended"] = user != null && !user.IsActive;
            }
            else
            {
                context.Items["IsSuspended"] = false;
            }

            await _next(context);
        }
    }
}
