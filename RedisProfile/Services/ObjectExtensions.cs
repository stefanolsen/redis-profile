using StackExchange.Redis;
using System;
using System.Linq;
using System.Reflection;

namespace RedisProfile.Services
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts a simple object to a list of Redis Hash entries.
        /// All object properties is casted to strings that can be stored in Redis.
        /// </summary>
        public static HashEntry[] ToHashEntries(this object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();

            var entries = properties
                .Where(x => x.GetValue(obj) != null)
                .Select(property => new HashEntry(property.Name, property.GetValue(obj)
                    .ToString())).ToArray();

            return entries;
        }

        /// <summary>
        /// Instantiates an object of T, by matching a list of hash entries to the object properties.
        /// </summary>
        public static T ConvertFromRedis<T>(this HashEntry[] hashEntries)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            var obj = Activator.CreateInstance(typeof(T));

            foreach (var property in properties)
            {
                HashEntry entry = hashEntries.FirstOrDefault(g => g.Name.ToString().Equals(property.Name));
                if (entry == null)
                {
                    continue;
                }

                property.SetValue(obj, Convert.ChangeType(entry.Value.ToString(), property.PropertyType));
            }

            return (T)obj;
        }
    }
}
