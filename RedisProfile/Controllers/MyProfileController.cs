using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RedisProfile.Services;

namespace RedisProfile.Controllers
{
    [Authorize(Policy = "LoggedInUser")]
    public class MyProfileController : Controller
    {
        private readonly CustomerDataService _customerDataService = new CustomerDataService();
        private readonly DummyCrmService _crmService = new DummyCrmService();

        public async Task<IActionResult> Index()
        {
            var claimsPrincipal = HttpContext.User;

            // Find and parse the user token GUID from claim.
            var userTokenString = claimsPrincipal.FindFirst("UserToken")?.Value;
            var userToken = Guid.Parse(userTokenString);

            // Look up user data in Redis.
            var userData = await _customerDataService.GetProfileDataAsync(userToken);

            // TODO: Handle a situation where no data exists.
            return View(userData);
        }
    }
}
