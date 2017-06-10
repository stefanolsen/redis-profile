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
            var customerDataService = new CustomerDataService();
            var userPrincipal = context.Principal;

            var userTokenString = userPrincipal.FindFirst("UserToken")?.Value;
            if (string.IsNullOrWhiteSpace(userTokenString))
            {
                // If the principal does not contain a user token, sign out the user.
                context.RejectPrincipal();
                await context.HttpContext.Authentication.SignOutAsync("Cookies");
                return;
            }

            // Validate that the user token (still) exists in Redis.
            var userToken = Guid.Parse(userTokenString);
            bool profileExists = await customerDataService.ValidateUserTokenExistsAsync(userToken);

            if (!profileExists)
            {
                // If the principal does not exist or is expired in Redis, sign out the user.
                context.RejectPrincipal();
                await context.HttpContext.Authentication.SignOutAsync("Cookies");
            }
        }
    }
}
