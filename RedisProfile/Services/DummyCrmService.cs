using System;

namespace RedisProfile.Services
{
    public class DummyCrmService
    {
        public bool Validate(string username, string password)
        {
            return true;
        }

        public BasicData GetBasicData(string username)
        {
            return new BasicData
            {
                UserId = 1210,
                FirstName = "Stefan",
                LastName = "Olsen",
                Email = "stefan@test.com",
                HasMarketingPermission = true,
                CreatedDate = new DateTime(2000, 1, 1)
            };
        }
    }
}
