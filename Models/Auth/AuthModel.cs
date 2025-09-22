namespace CoreHRAPI.Models.Auth
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
}
