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
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "  "))
                {
                    new DataContractJsonSerializer(typeof(T)).WriteObject(writer, obj);
                    writer.Flush();
                }
                string jsonString = Encoding.UTF8.GetString(ms.ToArray());
                return jsonString;
            }
        }

        public static void ConvertToJson<T>(FileInfo file, T obj)
        {
            var content = ConvertToJson(obj);
            File.WriteAllText(file.FullName, content);
        }

        public static async Task ConvertToJsonAsync<T>(FileInfo file, T obj)
        {
            var content = ConvertToJson(obj);
            await File.WriteAllTextAsync(file.FullName, content);
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
