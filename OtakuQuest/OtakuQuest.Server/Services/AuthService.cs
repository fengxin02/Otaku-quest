using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OtakuQuest.Server.Services
{
    public class AuthService
    {
        private readonly OtakuQuestDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(OtakuQuestDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ServiceResult<string>> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return ServiceResult<string>.Failure("This username is already taken");
            }

            var defaultAvatar = await _context.Items.FirstOrDefaultAsync(i => i.Name == "Default Avatar");
            if (defaultAvatar == null)
            {
                defaultAvatar = new Item { Name = "Default Avatar", Type = ItemType.Character, ImageAsset = "DefaultAvatar" };
                _context.Items.Add(defaultAvatar);
            }

            var defaultBackground = await _context.Items.FirstOrDefaultAsync(i => i.Name == "Default Background");
            if (defaultBackground == null)
            {
                defaultBackground = new Item { Name = "Default Background", Type = ItemType.Background, ImageAsset = "DefaultBackground" };
                _context.Items.Add(defaultBackground);
            }

            await _context.SaveChangesAsync();

            //new user 
            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), // Hash the password before storing
                EquippedAvatarId = defaultAvatar.Id,
                EquippedBackgroundId = defaultBackground.Id,
                Inventory = new List<UserItem>
                {
                    new UserItem { ItemId = defaultAvatar.Id },
                    new UserItem { ItemId = defaultBackground.Id }
                }
            };

            //save to database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Registration was succeed, welcome to OtakuQuest ");
        }

        public ServiceResult<AuthResponseDto> Login(LoginDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (user == null)
            {
                return ServiceResult<AuthResponseDto>.Failure("Invalid username or password");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return ServiceResult<AuthResponseDto>.Failure("Invalid username or password");
            }

            // Authentication successful, create claims for the user (collect the datas what token brings)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            //finds the key from appsettings.json and creates a symmetric security key for signing the token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            //creates signing credentials using the security key and the HMAC SHA256 algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1), // token valid for 1 day
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return ServiceResult<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = tokenString,
                Message = $"Welcome back: {user.Username} \nYour level is: {user.Level}"
            });
        }
    }
}
