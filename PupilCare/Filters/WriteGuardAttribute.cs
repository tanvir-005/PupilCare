using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PupilCare.Data;
using PupilCare.Models;
using System;
using System.Threading.Tasks;

namespace PupilCare.Filters
{
    /// <summary>
    /// Blocks all write (non-GET) operations when a school's subscription has expired.
    /// Reads are always allowed — users can view all existing data.
    /// SuperAdmin is always exempt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WriteGuardAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only apply guard on POST/PUT/DELETE (writes)
            var method = context.HttpContext.Request.Method;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var actionName = context.RouteData.Values["action"]?.ToString();
            if (actionName is "InitiatePayment" or "PaymentSuccess" or "PaymentFail" or "PaymentCancel")
            {
                await next();
                return;
            }

            // SuperAdmin is always exempt
            if (context.HttpContext.User.IsInRole("SuperAdmin"))
            {
                await next();
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

            var user = await userManager.GetUserAsync(context.HttpContext.User);
            if (user?.SchoolId == null)
            {
                await next();
                return;
            }

            var school = await dbContext.Schools.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == user.SchoolId);

            if (school == null)
            {
                await next();
                return;
            }

            bool subscriptionActive = school.SubscriptionExpiry.HasValue &&
                                      school.SubscriptionExpiry.Value > DateTime.UtcNow;

            if (!subscriptionActive)
            {
                // Block the write — redirect to subscription page with a message
                var controller = context.Controller as Microsoft.AspNetCore.Mvc.Controller;
                if (controller != null)
                {
                    controller.TempData["WriteGuardBlocked"] =
                        "Your subscription has expired. Please renew to make changes. You can still view all existing data.";
                }

                // Redirect to subscription page
                context.Result = new RedirectToActionResult("Subscription", "SchoolAdmin", null);
                return;
            }

            await next();
        }
    }
}
