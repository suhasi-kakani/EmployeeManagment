using System.Text;

namespace EmployeeManagment.Utilities
{
    public static class TokenHelper
    {
        public static string EncodeContinuationToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToBase64String(bytes);
        }

        public static string DecodeContinuationToken(string encodedToken)
        {
            if (string.IsNullOrEmpty(encodedToken)) return null;
            var bytes = Convert.FromBase64String(encodedToken);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
