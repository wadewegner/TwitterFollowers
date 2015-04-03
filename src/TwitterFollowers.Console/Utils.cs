using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitterOAuth.RestAPI.Exceptions;
using TwitterOAuth.RestAPI.Models;

namespace TwitterFollowers.Console
{
    public static class Utils
    {
        public static void DoWithRetry(Action action, int retryCount = 3)
        {
            while (true)
            {
                try
                {
                    action();
                    break; // success!
                }
                catch (AggregateException ae)
                {
                    ae.Handle((x) =>
                    {
                        if (x is RateLimitedException)
                        {
                            var ex = (RateLimitedException)x;
                            if (--retryCount == 0)
                            {
                                return false;
                            }

                            var rateLimitResetUtc = FromUnixTime(ex.RateLimit);
                            var span = rateLimitResetUtc.Subtract(DateTime.UtcNow.AddSeconds(10));
                            
                            System.Console.WriteLine("Sleeping for {0} until {1}", span, rateLimitResetUtc.ToLocalTime());
                            
                            Thread.Sleep((int)Math.Abs(span.TotalMilliseconds));
                        }
                        return true;
                    });
                }
            }
        }

        public static List<string> StoreIds(List<string> ids, string fileName)
        {
            File.WriteAllLines(fileName, ids);
            return ids;
        }


        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static string GetHeaderValue(HttpHeaders headers, string header)
        {
            IEnumerable<string> values;
            if (headers.TryGetValues(header, out values))
            {
                return values.First();
            }
            return null;
        }
    }
}
