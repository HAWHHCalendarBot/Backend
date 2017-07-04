using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Downloader
{
    internal static class Extensions
    {
        private static HttpClient GetHttpClient(string productName = "HAWHHCalendarBot", string productVersion = "1.0")
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));

            return client;
        }

        public static async Task<string> GetContent(this Uri uri)
        {
            using (var client = GetHttpClient())
            {
                return await client.GetStringAsync(uri);
            }
        }

        public static async Task<string> GetContent(this Uri uri, Encoding encoding)
        {
            using (var client = GetHttpClient())
            {
                var byteContent = await client.GetByteArrayAsync(uri);
                var content = encoding.GetString(byteContent);

                return content;
            }
        }

        public static async Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks)
        {
            return await Task.WhenAll(tasks);
        }
    }
}
