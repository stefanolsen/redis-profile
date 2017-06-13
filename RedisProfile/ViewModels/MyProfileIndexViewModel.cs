using System.Collections.Generic;
using RedisProfile.Services;

namespace RedisProfile.ViewModels
{
    public class MyProfileIndexViewModel
    {
        public BasicData BasicData { get; set; }
        public ICollection<SupportInquiry> SupportInquiries { get; set; }
    }
}
