namespace CoreHRAPI.Models.Configuration
{
    public class APIResponse<T>
    {
        public int code { get; set; }
        public string message { get; set; }
        public T? Data { get; set; }

        public static APIResponse<T> Success(string message = "Success", T? data = default)
        {
            return new APIResponse<T>
            {
                code = 200,
                message = message,
                Data = data
            };
        }

        public static APIResponse<T> Fail(string message = "Failed", int code = 500, T? data = default)
        {
            return new APIResponse<T>
            {
                code = code,
                message = message,
                Data = data
            };
        }
    }
}
