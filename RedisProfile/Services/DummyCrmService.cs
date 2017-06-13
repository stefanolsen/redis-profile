using System;
using System.Collections.Generic;

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

        public ICollection<SupportInquiry> GetSupportInquiries(string username)
        {
            return new List<SupportInquiry>
            {
                new SupportInquiry
                {
                    Id = 12,
                    IsClosed = false,
                    SubmittedDate = new DateTime(2017, 1, 1),
                    SubmitterEmail = "test@test.com",
                    SubmitterName = "Tester",
                    Description = "The bill is wrong."
                },
                new SupportInquiry
                {
                    Id = 80,
                    IsClosed = true,
                    SubmittedDate = new DateTime(2017, 2, 1),
                    SubmitterEmail = "test@test.com",
                    SubmitterName = "Tester",
                    Description = "I can not log in."
                },
                new SupportInquiry
                {
                    Id = 150,
                    IsClosed = false,
                    SubmittedDate = new DateTime(2017, 3, 1),
                    SubmitterEmail = "test@test.com",
                    SubmitterName = "Tester",
                    Description = "Something is wrong."
                }
            };
        }
    }
}
