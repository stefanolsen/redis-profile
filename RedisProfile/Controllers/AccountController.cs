using Microsoft.AspNetCore.Mvc;
using RedisProfile.Services;
using RedisProfile.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RedisProfile.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Mocked: call a CRM system to verify that the user can be logged in.
            var crmService = new DummyCrmService();
            bool loggedIn = crmService.Validate(model.UserName, model.Password);

            if (loggedIn)
            {
                // Mocked: load user data from a CRM system.
                var basicData = crmService.GetBasicData(model.UserName);
                var supportInquiries = crmService.GetSupportInquiries(model.UserName);

                // Generate a random user token for this user session.
                Guid userToken = Guid.NewGuid();

                var customerDataService = new CustomerDataService();

                // Store the user token - user id relation in Redis.
                await customerDataService.StoreUserToken(userToken, basicData.UserId);

                // Store the user data in Redis.
                await customerDataService.StoreBasicDataAsync(userToken, basicData);
                await customerDataService.StoreSupportInquiriesDataAsync(userToken, supportInquiries);

                // Store the user token as an identity claim.
                var claims = new List<Claim>
                {
                    new Claim("UserToken", userToken.ToString("N"), ClaimValueTypes.String, "http://testsite.local")
                };

                var userIdentity = new ClaimsIdentity(claims);
                var userPrincipal = new ClaimsPrincipal(userIdentity);

                // Sign in the user.
                await HttpContext.Authentication.SignInAsync("Cookies", userPrincipal);

                return RedirectToAction("Index", "MyProfile");
            }

            ModelState.AddModelError("", "Invalid login!");

            return View(model);
        }

        public async Task<IActionResult> LogOff()
        {
            // Try to clean up data in Redis, if possible.
            var claimsPrincipal = HttpContext.User;
            var userTokenString = claimsPrincipal.FindFirst("UserToken")?.Value;
            if (!string.IsNullOrWhiteSpace(userTokenString))
            {
                var userToken = Guid.Parse(userTokenString);

                var customerDataService = new CustomerDataService();
                await customerDataService.DeleteUserDataAsync(userToken);
            }

            // Sign out the user.
            await HttpContext.Authentication.SignOutAsync("Cookies");

            return RedirectToAction("Login", "Account");
        }
    }
}
