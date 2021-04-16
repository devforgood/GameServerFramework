using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class RedLock : IAsyncDisposable
    {
        private static TimeSpan lock_expire = new TimeSpan(0, 0, 5);

        string LockName;
        public bool IsAcquired = false;



        public async Task<bool> LockAsync(string key)
        {
            LockName = key;
            for (int i = 0; i < 100; ++i)
            {
                //Log.Information($"try lock {LockName}, {i}");
                var ret = await Cache.Instance.GetDatabase().StringSetAsync(key, 0, lock_expire, When.NotExists);
                if (ret == true)
                {
                    IsAcquired = true;
                    //Log.Information($"locked {LockName}");
                    return true;
                }

                await Task.Delay(100);
            }

            Log.Error($"LockAsync error {key}");
            return false;
        }

        public async Task UnlockAsync()
        {
            await Cache.Instance.GetDatabase().KeyDeleteAsync(LockName);
            //Log.Information($"unlock {LockName}");

        }

        public static async Task<RedLock> CreateLockAsync(string key)
        {
            var redLock = new RedLock();
            await redLock.LockAsync(key);
            return redLock; 
        }


        public async ValueTask DisposeAsync()
        {
            await UnlockAsync();
        }
    }
}