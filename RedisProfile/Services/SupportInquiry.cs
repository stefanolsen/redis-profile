using System;

namespace RedisProfile.Services
{
    public class SupportInquiry
    {
        public long Id { get; set; }
        public bool IsClosed { get; set; }
        public string Description { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string SubmitterEmail { get; set; }
        public string SubmitterName { get; set; }
    }
}
