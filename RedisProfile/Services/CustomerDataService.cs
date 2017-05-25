using System;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisProfile.Services
{
    public class CustomerDataService
    {
        private const string UserTokenKey = "usertokens";

        public CustomerDataService()
        {
        }

        public async Task<bool> ValidateProfileExistsAsync(Guid userToken, long userId)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                string tokenString = userToken.ToString("N");

                RedisValue idValue = await database.HashGetAsync(UserTokenKey, tokenString, CommandFlags.PreferSlave);

                return !idValue.IsNullOrEmpty &&
                    idValue.IsInteger &&
                    idValue == userId;
            }
        }

        public async Task DeleteProfileDataAsync(Guid userToken, long userId)
        {
            using (var connection = await GetConnection())
            {
                var database = connection.GetDatabase();

                string tokenString = userToken.ToString("N");

                await database.HashDeleteAsync(UserTokenKey, tokenString, CommandFlags.DemandMaster | CommandFlags.FireAndForget);
            }
        }

        private async static Task<ConnectionMultiplexer> GetConnection()
        {
            return await ConnectionMultiplexer.ConnectAsync("localhost");
        }
    }
}
