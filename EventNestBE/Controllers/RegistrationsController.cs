using EventNestBE.Data;
using EventNestBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

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

        [HttpPost]
        public async Task<ActionResult<Registration>> RegisterEvent(RegisterEventDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var targetEvent = await _context.Events.FindAsync(dto.EventId);
            if (targetEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện này!" });
            }

            var currentTime = DateTime.UtcNow;

            if (currentTime < targetEvent.RegistrationStartTime)
            {
                return BadRequest(new { message = "Chưa tới thời gian mở đăng ký cho sự kiện này!" });
            }

            if (currentTime > targetEvent.RegistrationEndTime)
            {
                return BadRequest(new { message = "Đã hết hạn đăng ký tham gia sự kiện này!" });
            }

            var currentAttendeesCount = await _context.Registrations
                .CountAsync(r => r.EventId == dto.EventId);

            if (currentAttendeesCount >= targetEvent.MaxAttendees)
            {
                return BadRequest(new { message = "Rất tiếc! Sự kiện này đã đạt số lượng tối đa." });
            }

            var alreadyRegistered = await _context.Registrations
                .AnyAsync(r => r.EventId == dto.EventId && r.StudentId == dto.StudentId);

            if (alreadyRegistered)
            {
                return BadRequest(new { message = "Sinh viên này đã đăng ký sự kiện này rồi!" });
            }

            var reg = new Registration
            {
                EventId = dto.EventId,
                StudentId = dto.StudentId,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Registrations.Add(reg);
            targetEvent.CurrentAttendees += 1;

            await _context.SaveChangesAsync();

            var student = await _context.Users.FindAsync(reg.StudentId);
            if (student != null && !string.IsNullOrEmpty(student.Email))
            {
                await _emailService.SendEmailAsync(
                    student.Email,
                    "Xác nhận đăng ký thành công!",
                    $"Chào {student.FullName}, bạn đã đăng ký thành công {targetEvent.Title}."
                );
            }

            return Ok(reg);
        }

        // 2. API Điểm danh sinh viên bằng QR (PUT: api/registrations/checkin)
        [Authorize(Roles = "Admin")]
        [HttpPut("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == dto.EventId && r.StudentId == dto.StudentId);

            if (registration == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin đăng ký của sinh viên này tại sự kiện!" });
            }

            if (registration.IsCheckedIn)
            {
                return BadRequest(new { message = "Sinh viên này đã được điểm danh từ trước rồi!" });
            }

            var targetEvent = await _context.Events.FindAsync(dto.EventId);
            if (targetEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện liên quan!" });
            }

            if (DateTime.UtcNow > targetEvent.EndTime)
            {
                return BadRequest(new { message = "Sự kiện này đã kết thúc, bạn không thể điểm danh được nữa!" });
            }

            registration.IsCheckedIn = true;
            registration.CheckInTime = DateTime.UtcNow;

            var student = await _context.Users.FindAsync(dto.StudentId);
            if (student != null)
            {
                student.TrainingPoints += targetEvent.TrainingPoints;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Điểm danh thành công! Sinh viên được cộng {targetEvent.TrainingPoints} điểm rèn luyện.",
                currentPoints = student?.TrainingPoints
            });
        }
        // 2. API Xuất danh sách điểm danh ra file Excel/CSV
        [Authorize(Roles = "Admin")]
        [HttpGet("event/{eventId}/export")]
        public async Task<IActionResult> ExportEventRegistrations(int eventId)
        {
            var targetEvent = await _context.Events.FindAsync(eventId);
            if (targetEvent == null) return NotFound("Không tìm thấy sự kiện!");

            var registrations = await _context.Registrations
                .Where(r => r.EventId == eventId)
                .Join(_context.Users,
                      reg => reg.StudentId,
                      usr => usr.Id,
                      (reg, usr) => new
                      {
                          usr.Mssv,
                          usr.FullName,
                          usr.Email,
                          reg.RegisteredAt,
                          reg.IsCheckedIn,
                          reg.CheckInTime
                      }).ToListAsync();

            var builder = new StringBuilder();

            builder.AppendLine("MSSV,Ho Ten,Email,Ngay Dang Ky,Trang Thai Check-in, Thoi Gian Check-in");

            foreach (var reg in registrations)
            {
                var checkInStatus = reg.IsCheckedIn ? "Da Check-in" : "Chua Check-in";
                var checkInTime = reg.CheckInTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "";
                var regTime = reg.RegisteredAt.ToString("dd/MM/yyyy HH:mm:ss");

                builder.AppendLine($"{reg.Mssv},{reg.FullName},{reg.Email},{regTime},{checkInStatus},{checkInTime}");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"DanhSachSinhVien_SuKien_{eventId}.csv");
        }
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetRegistrationsByEvent(
            int eventId,
            string? search = null,
            string? faculty = null,
            bool? isCheckedIn = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Registrations
                .Where(r => r.EventId == eventId)
                .Join(_context.Users,
                      reg => reg.StudentId,
                      usr => usr.Id,
                      (reg, usr) => new
                      {
                          RegistrationId = reg.Id,
                          reg.EventId,
                          reg.StudentId,
                          reg.RegisteredAt,
                          reg.IsCheckedIn,
                          reg.CheckInTime,
                          StudentName = usr.FullName,
                          StudentMssv = usr.Mssv,
                          StudentEmail = usr.Email,
                          Faculty = usr.Faculty
                      });

            var totalRegistrations = await query.CountAsync();
            var checkedInCount = await query.CountAsync(x => x.IsCheckedIn);
            var pendingCount = totalRegistrations - checkedInCount;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.StudentName.Contains(search) ||
                                         x.StudentMssv.Contains(search) ||
                                         x.StudentEmail.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(faculty))
            {
                query = query.Where(x => x.Faculty == faculty);
            }

            if (isCheckedIn.HasValue)
            {
                query = query.Where(x => x.IsCheckedIn == isCheckedIn.Value);
            }

            var totalFilteredItems = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                Summary = new
                {
                    Total = totalRegistrations,
                    CheckedIn = checkedInCount,
                    Pending = pendingCount
                },
                Pagination = new
                {
                    TotalItems = totalFilteredItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalFilteredItems / (double)pageSize)
                },
                Data = data
            });
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetRegistrationsByStudent(int studentId)
        {
            var registrations = await _context.Registrations
                .Where(r => r.StudentId == studentId)
                .Include(r => r.Event)
                .Select(r => new
                {
                    RegistrationId = r.Id,
                    r.EventId,
                    r.StudentId,
                    r.RegisteredAt,
                    r.IsCheckedIn,
                    EventTitle = r.Event.Title,
                    EventLocation = r.Event.Location,
                    EventStartTime = r.Event.StartTime,
                    EventEndTime = r.Event.EndTime,
                    EventBannerUrl = r.Event.BannerUrl
                })
                .ToListAsync();

            if (!registrations.Any())
            {
                return NotFound(new { message = "Sinh viên này chưa đăng ký sự kiện nào." });
            }

            return Ok(registrations);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin đăng ký cần hủy!" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && (userIdClaim == null || int.Parse(userIdClaim) != registration.StudentId))
            {
                return Forbid();
            }

            if (registration.IsCheckedIn)
            {
                return BadRequest(new { message = "Sinh viên đã check-in, không thể hủy đăng ký!" });
            }

            _context.Registrations.Remove(registration);
            var targetEvent = await _context.Events.FindAsync(registration.EventId);
            if (targetEvent != null && targetEvent.CurrentAttendees > 0)
            {
                targetEvent.CurrentAttendees -= 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã hủy đăng ký thành công!" });
        }
    }
}