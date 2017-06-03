using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace RedisProfile.Services
{
    public static class CustomPrincipalValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            // Pull database from registered DI services.
            var customerDataService = new CustomerDataService();
            var userPrincipal = context.Principal;

            var userTokenString = userPrincipal.FindFirst("UserToken")?.Value;
            //var userIdString = userPrincipal.FindFirst("UserId")?.Value;

            //if (string.IsNullOrWhiteSpace(userIdString) ||
            //    string.IsNullOrWhiteSpace(userTokenString))
            if (string.IsNullOrWhiteSpace(userTokenString))
            {
                context.RejectPrincipal();
                await context.HttpContext.Authentication.SignOutAsync("Cookie");
                return;
            }

            //var userId = int.Parse(userIdString);
            var userToken = Guid.Parse(userTokenString);

            bool profileExists = await customerDataService.ValidateUserTokenExistsAsync(userToken);

            if (!profileExists)
            {
                context.RejectPrincipal();
                await context.HttpContext.Authentication.SignOutAsync("Cookie");
            }
        }
    }
}
