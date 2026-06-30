using EventNestBE.Data;
using EventNestBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventNestBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // Thêm biến để đọc Key trong appsettings.json

        // Cập nhật lại Hàm khởi tạo
        public UsersController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (userExists) return BadRequest("Tài khoản (MSSV) này đã tồn tại!");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // --- CẬP NHẬT HÀM LOGIN ĐỂ TRẢ VỀ TOKEN ---
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginModel loginInfo)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginInfo.Password, user.PasswordHash))
            {
                return BadRequest("Tài khoản hoặc mật khẩu không chính xác!");
            }

            // 1. Tạo các thông tin sẽ giấu vào trong chiếc thẻ JWT (Claims)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role) // Lưu quyền (Student/Admin) vào thẻ
            };

            // 2. Lấy chìa khóa bí mật từ file appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Tiến hành in thẻ JWT (có thời hạn 1 ngày)
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            // 4. Chuyển thẻ thành chuỗi chữ và trả về cho người dùng
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = jwtToken, // Trả token về đây
                role = user.Role
            });
        }
        // 1. Xem hồ sơ của MỘT người dùng (GET: api/users/{id})
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng này!" });
            }

            return Ok(user);
        }

        // 2. Cập nhật thông tin cá nhân (PUT: api/users/{id})
        // Có thẻ [Authorize] để bắt buộc phải đăng nhập mới được sửa thông tin
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest(new { message = "ID không khớp!" });
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng!" });
            }

            // Chỉ cho phép cập nhật một số thông tin cơ bản
            existingUser.FullName = updatedUser.FullName;
            existingUser.Email = updatedUser.Email;

            // LƯU Ý: Không cho phép cập nhật Username, Password hay Role ở hàm này!

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin thành công!", data = existingUser });
        }

        // 3. Xóa người dùng (DELETE: api/users/{id})
        // ĐẶC BIỆT: Chỉ có tài khoản mang quyền "Admin" mới gọi được hàm này
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng!" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa người dùng thành công!" });
        }
        [Authorize] // Bắt buộc phải đăng nhập mới được đổi
            [HttpPut("change-password/{id}")]
            public async Task<IActionResult> ChangePassword(int id, ChangePasswordModel model)
            {
            // 1. Tìm user trong database
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng!" });
            }

            // 2. Kiểm tra xem mật khẩu cũ có đúng không
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });
            }

            // 3. Mã hóa mật khẩu mới và lưu lại
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
    
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
            }

            public class UserLoginModel
            {
                public string Username { get; set; }
                public string Password { get; set; }
            }
            public class ChangePasswordModel
            {
                public string CurrentPassword { get; set; } = string.Empty;
                public string NewPassword { get; set; } = string.Empty;
            }
        }