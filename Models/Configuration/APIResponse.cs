namespace KYCAPI.Models.Configuration
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

    // Non-generic APIResponse for KYC controllers
    public class APIResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }

        public static APIResponse CreateSuccess(string message = "Success", object? data = null)
        {
            return new APIResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static APIResponse CreateFailure(string message = "Failed", object? data = null)
        {
            return new APIResponse
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }
}
