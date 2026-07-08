using EventNestBE.Data;
using EventNestBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventNestBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 0. Lấy danh sách sự kiện ĐANG DIỄN RA ---
        [HttpGet("ongoing")]
        public async Task<IActionResult> GetOngoingEvents()
        {
            var currentTime = DateTime.UtcNow;

            var ongoingEvents = await _context.Events
                .AsNoTracking() // Tối ưu: Báo cho EF biết chỉ đọc, không cần track thay đổi bộ nhớ
                .Where(e => e.Status == "Published" && e.StartTime <= currentTime && e.EndTime >= currentTime)
                .OrderBy(e => e.EndTime)
                .ToListAsync();

            return Ok(ongoingEvents);
        }

        // --- 1. Lấy danh sách toàn bộ sự kiện (Có phân trang, Search, Lọc) ---
        [HttpGet]
        public async Task<IActionResult> GetEvents(int page = 1, int pageSize = 10, string? search = null)
        {
            // Chống crash nếu Frontend gửi dữ liệu bậy
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Events.AsNoTracking().AsQueryable();

            // Lọc theo Role
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                query = query.Where(e => e.Status == "Published");
            }

            // Lọc theo Search (Chuyển ToLower để tìm kiếm không phân biệt hoa thường một cách tương đối)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(lowerSearch) ||
                                         e.Location.ToLower().Contains(lowerSearch));
            }

            var totalItems = await query.CountAsync();

            var events = await query
                .OrderByDescending(e => e.StartTime) // Thường người dùng muốn thấy event mới nhất lên đầu thay vì lấy bừa
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = events
            });
        }

        // --- 2. Lấy chi tiết một sự kiện ---
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var myEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (myEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện này!" });
            }

            // Bịt lỗ hổng bảo mật: Không cho User thường dùng ID trực tiếp truy cập vào sự kiện đang Draft
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && myEvent.Status != "Published")
            {
                return Forbid(); // Trả về 403 hoặc có thể dùng NotFound để ẩn hoàn toàn sự tồn tại
            }

            return Ok(myEvent);
        }

        // --- 3. Tạo sự kiện ---
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] Event myEvent)
        {
            // Ép cứng để tránh client truyền láo số người tham gia ban đầu
            myEvent.CurrentAttendees = 0;

            _context.Events.Add(myEvent);
            await _context.SaveChangesAsync();

            // Chuẩn RESTful: POST xong trả về code 201 (Created) kèm đường dẫn tới tài nguyên vừa tạo
            return CreatedAtAction(nameof(GetEvent), new { id = myEvent.Id }, myEvent);
        }

        // --- 4. Cập nhật sự kiện ---
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            if (id != updatedEvent.Id)
            {
                return BadRequest(new { message = "ID sự kiện trong URL và Body không khớp!" });
            }

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện cần sửa!" });
            }

            // Cập nhật an toàn (Không cập nhật CurrentAttendees, OrganizerId)
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Address = updatedEvent.Address;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            existingEvent.RegistrationStartTime = updatedEvent.RegistrationStartTime;
            existingEvent.RegistrationEndTime = updatedEvent.RegistrationEndTime;
            existingEvent.MaxAttendees = updatedEvent.MaxAttendees;
            existingEvent.BannerUrl = updatedEvent.BannerUrl;
            existingEvent.Status = updatedEvent.Status;
            existingEvent.TrainingPoints = updatedEvent.TrainingPoints;
            existingEvent.PenaltyPoints = updatedEvent.PenaltyPoints; // Bản cũ anh đang sót dòng này

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = "Cơ sở dữ liệu đang bận, vui lòng thử lại sau." });
            }

            return Ok(new { message = "Cập nhật sự kiện thành công!", data = existingEvent });
        }

        // --- 5. Xóa sự kiện ---
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            // Phải include Registrations để check trước khi xóa
            var myEvent = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (myEvent == null)
            {
                return NotFound(new { message = "Không tìm thấy sự kiện cần xóa!" });
            }

            // Bịt lỗi văng Database Exception do Foreign Key
            if (myEvent.Registrations != null && myEvent.Registrations.Any())
            {
                return BadRequest(new
                {
                    message = "Không thể xóa cứng do sự kiện này đã có sinh viên đăng ký. Vui lòng cập nhật trạng thái thành 'Canceled' (Hủy) thay vì xóa."
                });
            }

            _context.Events.Remove(myEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sự kiện thành công!" });
        }
    }
}