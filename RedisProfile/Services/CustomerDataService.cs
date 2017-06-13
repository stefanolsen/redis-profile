using System;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisProfile.Services
{
    public class CustomerDataService
    {
        private static readonly TimeSpan DefaultTokenExpiryMinutes = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultDataExpiryMinutes = TimeSpan.FromMinutes(120);

        private const string UserTokenKeyFormat = "tokens:{0}";
        private const string BasicDataKeyFormat = "basicdata:{0}";
        private const string SupportInquiriesKeyFormat = "supportinquiries:{0}";
        private const string SupportInquiryKeyFormat = "supportinquiry:{0}";

        /// <summary>
        /// Validates that the user token exists and refreshes the user token TTL.
        /// </summary>
        public async Task<bool> ValidateUserTokenExistsAsync(Guid userToken)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken, refreshTtl: true);

                // If the user id exists, we can consider the session to be active.
                bool exists = userId.HasValue;

                return exists;
            }
        }

        /// <summary>
        /// Deletes all user entries.
        /// </summary>
        public async Task DeleteUserDataAsync(Guid userToken)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return;
                }

                // Generate a user token key string from the GUID.
                string tokenString = userToken.ToString("N");

                // Generate a specific key for the user's token.
                string tokenKey = string.Format(UserTokenKeyFormat, tokenString);

                // Generate a specific key for the user's profile hash.
                string basicDataKey = string.Format(BasicDataKeyFormat, userId);

                // Delete the entries from Redis.
                await database.KeyDeleteAsync(tokenKey, CommandFlags.DemandMaster | CommandFlags.FireAndForget);
                await database.KeyDeleteAsync(basicDataKey, CommandFlags.FireAndForget);
            }
        }

        /// <summary>
        /// Tries to get the basic data of a specific user.
        /// </summary>
        public async Task<BasicData> GetBasicDataAsync(Guid userToken)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return null;
                }

                // Generate a specific key for the user's profile hash.
                string basicDataKey = string.Format(BasicDataKeyFormat, userId);

                // Get the profile hash entries for the user token.
                var hashes = await database.HashGetAllAsync(basicDataKey);

                // Reset the key TTL (expiration), for sliding expiration like in regular session state.
                await database.KeyExpireAsync(basicDataKey, DefaultDataExpiryMinutes);

                // Convert the Redis hash entries into properties on a BasicData instance.
                var data = hashes.ConvertFromRedis<BasicData>();

                return data;
            }
        }

        /// <summary>
        /// Tries to get a collection of support inquiries related of a specific user.
        /// </summary>
        public async Task<ICollection<SupportInquiry>> GetSupportInquiriesAsync(Guid userToken, int offset, int count)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return null;
                }

                // Generate a specific key for the user's inquiry list.
                string inquiriesListKey = string.Format(SupportInquiriesKeyFormat, userId);

                // Reset the key TTL (expiration), for sliding expiration like in regular session state.
                await database.KeyExpireAsync(inquiriesListKey, DefaultDataExpiryMinutes);

                // Get the id's of all inquiries for the user token.
                RedisValue[] redisValues = database.ListRange(inquiriesListKey, offset, offset + count);
                if (redisValues == null)
                {
                    return null;
                }

                var inquiries = new List<SupportInquiry>();

                foreach (var redisValue in redisValues)
                {
                    long inquiryId = (long)redisValue;

                    // Generate a specific key for the inquiry hash.
                    string inquiryDataKey = string.Format(SupportInquiryKeyFormat, inquiryId);

                    // Get the inquiry hash entries for the inquiry id.
                    var hashes = await database.HashGetAllAsync(inquiryDataKey);

                    // Convert the Redis hash entries into properties on a SupportInquiry instance.
                    var inquiry = hashes.ConvertFromRedis<SupportInquiry>();

                    inquiries.Add(inquiry);
                }

                return inquiries;
            }
        }

        /// <summary>
        /// Stores the user token - user id relation.
        /// </summary>
        public async Task StoreUserToken(Guid userToken, long userId)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Generate a user token key from the GUID.
                string tokenString = userToken.ToString("N");

                // Generate a specific key for the user's profile hash.
                string tokenKey = string.Format(UserTokenKeyFormat, tokenString);

                // Store the user id under the user token key, and set a TTL on the key.
                await database.StringSetAsync(tokenKey, userId, DefaultTokenExpiryMinutes, When.Always, CommandFlags.DemandMaster);
            }
        }

        /// <summary>
        /// Tries to store an instance of BasicData for a user, identified by a user token.
        /// </summary>
        public async Task StoreBasicDataAsync(Guid userToken, BasicData data)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return;
                }

                // Convert basic data properties to Redis hash entries.
                var hashes = data.ToHashEntries();

                // Generate a specific key for the user's profile hash.
                string basicDataKey = string.Format(BasicDataKeyFormat, userId);

                // Store the hash entries and set a TTL (expiration) on the key.
                await database.HashSetAsync(basicDataKey, hashes);
                await database.KeyExpireAsync(basicDataKey, DefaultDataExpiryMinutes);
            }
        }

        /// <summary>
        /// Tries to store a collction of SupportInquiry instances, identified by a user token.
        /// </summary>
        public async Task StoreSupportInquiriesDataAsync(Guid userToken, ICollection<SupportInquiry> inquiries)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Try to look up the user id by the user token.
                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return;
                }

                // Generate a specific key for the user's inquiry list.
                string inquiriesListKey = string.Format(SupportInquiriesKeyFormat, userId);

                // Remove a potential existing list key and its values (to avoid duplicates).
                await database.KeyDeleteAsync(inquiriesListKey);

                foreach (var inquiry in inquiries)
                {
                    // Convert inquiry properties to Redis hash entries.
                    var hashes = inquiry.ToHashEntries();

                    // Generate a specific key for the inquiry hash.
                    string inquiryDataKey = string.Format(SupportInquiryKeyFormat, inquiry.Id);
                    
                    // Store the hash entries and set a TTL (expiration) on the key.
                    await database.HashSetAsync(inquiryDataKey, hashes);

                    // Store the ID of this inquiry in a list of inquiry IDs.
                    await database.ListRightPushAsync(inquiriesListKey, inquiry.Id);
                }

                // Set a TTL (expiration) on the list key.
                await database.KeyExpireAsync(inquiriesListKey, DefaultDataExpiryMinutes);
            }
        }

        /// <summary>
        /// Tries to look up a user id given a user token GUID. If specified, it also refreshes the current TTL.
        /// </summary>
        private static async Task<long?> GetUserId(IDatabaseAsync database, Guid userToken, bool refreshTtl = false)
        {
            string tokenString = userToken.ToString("N");
            string tokenKey = string.Format(UserTokenKeyFormat, tokenString);

            // Look up a user id string.
            // This is a string per token, as these can be expired independently.
            RedisValue idValue = await database.StringGetAsync(tokenKey);

            bool exists = !idValue.IsNullOrEmpty;
            if (!exists)
            {
                return null;
            }

            if (refreshTtl)
            {
                // If the user exists and the caller requests so, reset the key TTL (expiration).
                // This make sliding expiration work like in regular session state.
                await database.KeyExpireAsync(tokenKey, DefaultTokenExpiryMinutes, CommandFlags.FireAndForget);
            }

            return (long)idValue;
        }

        private static async Task<ConnectionMultiplexer> GetConnection()
        {
            return await ConnectionMultiplexer.ConnectAsync("localhost");
        }
    }
}
