namespace KYCAPI.Models.Auth
{
    public class AuthModel
    {
        
    }

    public class LoginDto
    {
       
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class GenerateBcryptRequest
    {
        public string PlainText { get; set; }
        public string Username { get; set; }
    }

    public class VerifyBcryptRequest
    {
        public string PlainText { get; set; }
        public string BcryptHash { get; set; }
    }
}
