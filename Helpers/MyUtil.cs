using System.Text;
using System.Text.RegularExpressions;

namespace TechStore.Helpers
{
    public class MyUtil
    {
        public static string ToUrlFriendly(string title)
        {
            if (title == null) return "";

            // const string prepositions = "a an the at by for in of on to up and as but or nor yet";
            var toLower = title.ToLower();
            toLower = Regex.Replace(toLower, @"[^a-z0-9\s-]", ""); // Xóa ký tự đặc biệt
            toLower = Regex.Replace(toLower, @"\s+", " ").Trim(); // Xóa khoảng trắng thừa
            toLower = Regex.Replace(toLower, @"\s", "-"); // Thay khoảng trắng bằng gạch ngang

            return toLower;
        }
    }
}