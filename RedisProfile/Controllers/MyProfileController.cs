using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RedisProfile.Services;
using RedisProfile.ViewModels;

namespace RedisProfile.Controllers
{
    [Authorize(Policy = "LoggedInUser")]
    public class MyProfileController : Controller
    {
        private readonly CustomerDataService _customerDataService = new CustomerDataService();

        public async Task<IActionResult> Index()
        {
            var claimsPrincipal = HttpContext.User;

            // Find and parse the user token GUID from claim.
            // TODO: This could be abstracted away, for instance to a base controller or a helper class.
            var userTokenString = claimsPrincipal.FindFirst("UserToken")?.Value;
            var userToken = Guid.Parse(userTokenString);


            // Instantiate a view model with data from Redis.
            var viewModel = new MyProfileIndexViewModel
            {
                BasicData = await _customerDataService.GetBasicDataAsync(userToken),
                SupportInquiries = await _customerDataService.GetSupportInquiriesAsync(userToken, 0, 20)
            };

            // TODO: Handle a situation where no data exists.
            return View(viewModel);
        }
    }
}
