using System.Text.Json;

namespace TechStore.Helpers
{
    public static class MySessionHelper
    {
        // Lưu đối tượng vào Session
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Lấy đối tượng từ Session
        // Thêm dấu ? vào sau T để cho phép trả về null nếu không tìm thấy key
        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}