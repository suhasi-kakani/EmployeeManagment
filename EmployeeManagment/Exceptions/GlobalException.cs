namespace EmployeeManagment.Exceptions
{
    public class GlobalException : Exception
    {
        public int StatusCode { get; }

        public GlobalException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
