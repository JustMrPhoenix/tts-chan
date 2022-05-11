using System;
using System.Threading;
using System.Threading.Tasks;

namespace TTS_Chan.Utils
{
    public static class Retry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int retryCount)
        {
            try
            {
                return action();
            }
            catch when (retryCount != 0)
            {
                Thread.Sleep(retryInterval);
                return Do(action, retryInterval, --retryCount);
            }
        }

        public static async Task<T> DoAsync<T>(Func<Task<T>> action, TimeSpan retryInterval, int retryCount)
        {
            try
            {
                return await action();
            }
            catch when (retryCount != 0)
            {
                await Task.Delay(retryInterval);
                return await DoAsync(action, retryInterval, --retryCount);
            }
        }
    }
}
