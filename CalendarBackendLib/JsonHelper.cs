using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBackendLib
{
    public static class JsonHelper
    {
        public static string ConvertToJson<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                string jsonString = Encoding.UTF8.GetString(ms.ToArray());
                return jsonString;
            }
        }

        public static T ConvertFromJson<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }

        public static T ConvertFromJson<T>(FileInfo file)
        {
            var content = File.ReadAllText(file.FullName);
            return ConvertFromJson<T>(content);
        }

        public static async Task<T> ConvertFromJsonAsync<T>(FileInfo file)
        {
            var content = await File.ReadAllTextAsync(file.FullName);
            return ConvertFromJson<T>(content);
        }
    }
}
