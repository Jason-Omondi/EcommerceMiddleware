using EcommerceMiddleware.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EcommerceMiddleware.Controllers.DataTransfer
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataTransferController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        public DataTransferController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_SUCCESS,
                    "User registered successfully!",
                    null,
                    true
                ));
            }

            return BadRequest(new DefaultConfigs.DefaultResponse(
                DefaultConfigs.STATUS_ERROR,
                "Registration failed",
                null,
                result.Errors,
                false
            ));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Input validation
                if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Invalid input: Email and Password are required",
                        null
                    ));
                }

                // Attempt sign-in
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    isPersistent: false,
                    lockoutOnFailure: true // Changed to true for security
                );

                // Successful login
                if (result.Succeeded)
                {
                    var token = GenerateJwtToken(model.Email);
                    return Ok(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_SUCCESS,
                        "Login successful",
                        token
                    ));
                }

                // Specific failure scenarios
                if (result.IsLockedOut)
                {
                    return Unauthorized(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Account is locked. Please try again later or reset your password.",
                        null
                    ));
                }

                if (result.IsNotAllowed)
                {
                    return Unauthorized(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Login not allowed. Please confirm your email or contact support.",
                        null
                    ));
                }

                // Generic unauthorized response
                return Unauthorized(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_ERROR,
                    "Invalid login credentials",
                    null
                ));
            }
            catch (Exception ex)
            {
                // Log the full exception
               // _logger.LogError(ex, "Login attempt failed for email: {Email}", model?.Email);

                // Return a generic 500 error response
                return StatusCode(500, new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_ERROR,
                    $"An unexpected error occurred while processing request!",
                    ex.ToString(),
                   // null,
                    false
                ));
            }
        }


        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult RefreshToken([FromBody] TokenRefreshModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model?.Token))
                {
                    return Unauthorized(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Token is required.",
                        null
                    ));
                }

                var principal = GetPrincipalFromExpiredToken(model.Token);
                if (principal == null)
                {
                    return Unauthorized(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Invalid token.",
                        null
                    ));
                }

                // Extract email from the claims (you don't need to pass email explicitly in the body)
                var email = principal.Identity.Name;
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Unauthorized(new DefaultConfigs.DefaultResponse(
                        DefaultConfigs.STATUS_ERROR,
                        "Email not found in the token.",
                        null
                    ));
                }

                var newToken = GenerateJwtToken(email);
                return Ok(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_SUCCESS,
                    "Request Processed successfully!",
                    newToken,
                    newToken.ToString(),
                    true
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_ERROR,
                    "An unexpected error occurred while processing request!",
                    ex.ToString(),
                    null,
                    false
                ));
            }
        }




        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_FAIL,
                    "User not found",
                    null
                ));
            }

            user.Email = model.Email ?? user.Email;
            user.UserName = model.UserName ?? user.UserName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_SUCCESS,
                    "Profile updated successfully",
                    null,
                    null,
                    true
                ));
            }

            return BadRequest(new DefaultConfigs.DefaultResponse(
                DefaultConfigs.STATUS_ERROR,
                "Profile update failed",
                null,
                result.Errors,
                false
            ));
        }


        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_FAIL,
                    "User not found",
                    null
                ));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_SUCCESS,
                    "Password changed successfully",
                    null
                ));
            }

            return BadRequest(new DefaultConfigs.DefaultResponse(
                DefaultConfigs.STATUS_ERROR,
                "Password change failed",
                null,
                result.Errors
            ));
        }


        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new DefaultConfigs.DefaultResponse(
                    DefaultConfigs.STATUS_FAIL,
                    "User not found",
                    null
                ));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Send the token via email (not implemented here)

            return Ok(new DefaultConfigs.DefaultResponse(
                DefaultConfigs.STATUS_SUCCESS,
                "Password reset token sent",
                null
            ));
        }

        //functions
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                // Log token before decoding
               // _logger.LogInformation($"Decoding token: {token}");

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false // Allow expired tokens
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex)
            {
                // Log the full exception to investigate
                //_logger.LogError(ex, "Error decoding token");
                throw;  // rethrow exception for further handling
            }
        }


        private string GenerateJwtToken(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentNullException("email", "Email cannot be null or empty");
            }

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:TokenExpiryMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // Profile Management can be added here (GET/PUT)
    }
}
