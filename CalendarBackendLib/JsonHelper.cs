using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CalendarBackendLib
{
    public static class JsonHelper
    {
        public static string ConvertObjectToJSon<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                string jsonString = Encoding.UTF8.GetString(ms.ToArray());
                return jsonString;
            }
        }

        public static T ConvertJSonToObject<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }
    }
}
