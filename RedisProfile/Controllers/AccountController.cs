﻿using Microsoft.AspNetCore.Mvc;
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
            bool loggedIn = true;

            if (loggedIn)
            {
                // Mocked: load user data from a CRM system.
                var data = new BasicData
                {
                    UserId = 1210,
                    FirstName = "Stefan",
                    LastName = "Olsen",
                    Email = "stefan@test.com",
                    HasMarketingPermission = true,
                    CreatedDate = new DateTime(2000, 1, 1)
                };

                Guid userToken = Guid.NewGuid();

                var customerDataService = new CustomerDataService();
                await customerDataService.StoreUserToken(userToken, data.UserId);
                await customerDataService.StoreProfileDataAsync(userToken, data);

                var claims = new List<Claim>
                {
                    new Claim("UserToken", userToken.ToString("N"), ClaimValueTypes.String, "http://testsite.local")
                };

                var userIdentity = new ClaimsIdentity(claims);
                var userPrincipal = new ClaimsPrincipal(userIdentity);

                await HttpContext.Authentication.SignInAsync("Cookie", userPrincipal);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid login!");

            return View(model);
        }

        public async Task<IActionResult> LogOff()
        {
            var claimsPrincipal = HttpContext.User;
            var userTokenString = claimsPrincipal.FindFirst("UserToken")?.Value;
            if (!string.IsNullOrWhiteSpace(userTokenString))
            {
                var userToken = Guid.Parse(userTokenString);

                var customerDataService = new CustomerDataService();
                await customerDataService.DeleteUserDataAsync(userToken);
            }

            await HttpContext.Authentication.SignOutAsync("Cookie");

            return RedirectToAction("Login", "Account");
        }
    }
}