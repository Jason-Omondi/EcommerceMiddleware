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
