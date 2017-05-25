using System;
namespace RedisProfile.Services
{
    public class BasicData
    {
        public long UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string AddressLine1 { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public bool HasMarketingPermission { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
