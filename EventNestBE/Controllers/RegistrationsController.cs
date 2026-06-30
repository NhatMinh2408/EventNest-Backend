using EventNestBE.Data;
using EventNestBE.Models;
using EventNestBE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventNestBE.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public RegistrationsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // 1. API Sinh viên bấm đăng ký tham gia sự kiện (POST: api/registrations)
        [HttpPost]
        public async Task<ActionResult<Registration>> RegisterEvent(Registration reg)
        {
            // --- LOGIC MỚI: KIỂM TRA SỐ LƯỢNG TỐI ĐA ---
            // 1.1 Tìm sự kiện xem có tồn tại không
            var targetEvent = await _context.Events.FindAsync(reg.EventId);
            if (targetEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện này!" });
            }

            // 1.2 Đếm số lượng người ĐÃ đăng ký sự kiện này
            var currentAttendeesCount = await _context.Registrations
                .CountAsync(r => r.EventId == reg.EventId);

            // 1.3 Kiểm tra nếu đã bằng hoặc vượt quá MaxAttendees thì chặn lại
            if (currentAttendeesCount >= targetEvent.MaxAttendees)
            {
                return BadRequest(new { message = "Rất tiếc! Sự kiện này đã đạt số lượng sinh viên đăng ký tối đa." });
            }
            // -------------------------------------------

            // KIỂM TRA TRÙNG LẶP 
            var alreadyRegistered = await _context.Registrations
                .AnyAsync(r => r.EventId == reg.EventId && r.StudentId == reg.StudentId);

            if (alreadyRegistered)
            {
                return BadRequest(new { message = "Sinh viên này đã đăng ký sự kiện này rồi!" });
            }

            // NẾU MỌI THỨ ĐỀU HỢP LỆ THÌ CHO PHÉP ĐĂNG KÝ
            _context.Registrations.Add(reg);
            await _context.SaveChangesAsync();

            var student = await _context.Users.FindAsync(reg.StudentId);
            var eventInfo = await _context.Events.FindAsync(reg.EventId);

            if (student != null && !string.IsNullOrEmpty(student.Email))
            {
                await _emailService.SendEmailAsync(
                    student.Email,
                    "Xác nhận đăng ký sự kiện thành công!",
                    $"Chào {student.FullName}, bạn đã đăng ký thành công sự kiện {eventInfo.Title}."
                );
            }
            return Ok(reg);
        }

        // 2. API Điểm danh sinh viên (PUT: api/registrations/checkin)
        [HttpPut("checkin")]
        public async Task<IActionResult> CheckIn(int eventId, int studentId)
        {
            // Tìm bản ghi đăng ký của sinh viên tại sự kiện đó
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.StudentId == studentId);

            if (registration == null)
            {
                return NotFound("Không tìm thấy thông tin đăng ký của sinh viên này tại sự kiện!");
            }

            // Tiến hành điểm danh
            registration.IsCheckedIn = true;
            registration.CheckInTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Điểm danh thành công!");
        }
        // 3. API Lấy danh sách tất cả sinh viên đăng ký MỘT sự kiện (GET: api/registrations/event/{eventId})
        // Dành cho Ban tổ chức: Xuất danh sách, kiểm tra số lượng
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrationsByEvent(int eventId)
        {
            var registrations = await _context.Registrations
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            if (!registrations.Any())
            {
                return NotFound(new { message = "Sự kiện này chưa có ai đăng ký." });
            }

            return Ok(registrations);
        }

        // 4. API Lấy lịch sử sự kiện đã đăng ký của MỘT sinh viên (GET: api/registrations/student/{studentId})
        // Dành cho Người dùng: Hiển thị ở màn hình "Vé của tôi" / "Lịch sử tham gia"
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrationsByStudent(int studentId)
        {
            var registrations = await _context.Registrations
                .Where(r => r.StudentId == studentId)
                .ToListAsync();

            if (!registrations.Any())
            {
                return NotFound(new { message = "Sinh viên này chưa đăng ký sự kiện nào." });
            }

            return Ok(registrations);
        }

        // 5. API Hủy đăng ký sự kiện (DELETE: api/registrations/{id})
        // Dành cho Sinh viên bấm hủy khi không thể tham gia
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin đăng ký cần hủy!" });
            }

            // (Tùy chọn) Thêm logic kiểm tra: Nếu đã Check-in rồi thì không cho hủy
            if (registration.IsCheckedIn)
            {
                return BadRequest(new { message = "Sinh viên đã check-in, không thể hủy đăng ký!" });
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã hủy đăng ký thành công!" });
        }
    }
}