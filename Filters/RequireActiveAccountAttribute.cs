using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JAA.Filters
{
    /// <summary>
    /// Blocks suspended customers from performing write/create operations.
    /// Apply to any action that creates or modifies operational data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireActiveAccountAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Items["IsSuspended"] is true)
            {
                if (context.Controller is Controller ctrl)
                    ctrl.TempData["SuspendedAction"] = true;

                context.Result = new RedirectToActionResult("Dashboard", "Customer", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
