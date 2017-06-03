using System;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisProfile.Services
{
    public class CustomerDataService
    {
        private const int DefaultTokenExpiryMinutes = 5;
        private const int DefaultDataExpiryMinutes = 120;

        private const string NextUserKeyKey = "next_user_key";
        private const string UserTokenKeyFormat = "tokens:{0}";
        private const string ProfileKeyFormat = "profiles:{0}";

        public async Task<bool> ValidateUserTokenExistsAsync(Guid userToken)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                long? userId = await GetUserId(database, userToken, refreshTtl: true);
                bool exists = userId.HasValue;

                return exists;
            }
        }

        public async Task DeleteUserDataAsync(Guid userToken)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return;
                }

                string tokenString = userToken.ToString("N");
                string tokenKey = string.Format(UserTokenKeyFormat, tokenString);
                string profileKey = string.Format(ProfileKeyFormat, userId);

                await database.KeyDeleteAsync(tokenKey, CommandFlags.DemandMaster | CommandFlags.FireAndForget);
                await database.KeyDeleteAsync(profileKey, CommandFlags.FireAndForget);
            }
        }

        public async Task StoreUserToken(Guid userToken, long userId)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                // Generate a user token key (including the Guid).
                string tokenString = userToken.ToString("N");
                string tokenKey = string.Format(UserTokenKeyFormat, tokenString);

                // Store the user id under the user token key.
                // Set it to expire after N minutes of not being accessed.
                await database.StringSetAsync(
                    tokenKey, userId, TimeSpan.FromMinutes(DefaultTokenExpiryMinutes),
                    When.Always, CommandFlags.DemandMaster);
            }
        }

        public async Task StoreProfileDataAsync(Guid userToken, BasicData data)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                long? userId = await GetUserId(database, userToken);
                if (!userId.HasValue)
                {
                    return;
                }

                string profileKey = string.Format(ProfileKeyFormat, userId);
                var hashes = data.ToHashEntries();

                await database.HashSetAsync(profileKey, hashes);
                await database.KeyExpireAsync(profileKey, TimeSpan.FromMinutes(DefaultDataExpiryMinutes));
            }
        }

        private async Task<long?> GetUserId(IDatabase database, Guid userToken, bool refreshTtl = false)
        {
            string tokenString = userToken.ToString("N");
            string tokenKey = string.Format(UserTokenKeyFormat, tokenString);

            RedisValue idValue = await database.StringGetAsync(tokenKey);

            bool exists = !idValue.IsNullOrEmpty;
            if (exists)
            {
                if (refreshTtl)
                {
                    await database.KeyExpireAsync(tokenKey, TimeSpan.FromMinutes(DefaultTokenExpiryMinutes), CommandFlags.FireAndForget);
                }

                return (long)idValue;
            }

            return null;
        }

        private async static Task<ConnectionMultiplexer> GetConnection()
        {
            return await ConnectionMultiplexer.ConnectAsync("localhost");
        }
    }
}
