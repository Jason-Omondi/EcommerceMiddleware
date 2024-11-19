using Microsoft.AspNetCore.Identity;

public class User : IdentityUser
{
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class ChangePasswordModel
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class UpdateProfileModel
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string ProfilePhoto { get; set; } // Base64 encoded string
}

public class TokenRefreshModel
{
    public string Token { get; set; }
    //public string Email { get; set; }
}

