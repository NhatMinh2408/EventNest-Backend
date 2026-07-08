    using EventNestBE.Data;
    using EventNestBE.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;

    namespace EventNestBE.Controllers
    {
        [Route("api/[controller]")] // Đường dẫn API sẽ là: api/events
        [ApiController]
        public class EventsController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            // Gọi DbContext vào đây để tương tác với SQL Server
            public EventsController(ApplicationDbContext context)
            {
                _context = context;
            }
      [HttpGet("ongoing")]
        public async Task<IActionResult> GetOngoingEvents()
        {
            var currentTime = DateTime.UtcNow;

            // Điều kiện đang diễn ra: Thời gian bắt đầu <= Hiện tại và Hiện tại <= Thời gian kết thúc
            var ongoingEvents = await _context.Events
                .Where(e => e.StartTime <= currentTime && e.EndTime >= currentTime)
                .OrderBy(e => e.EndTime) // Ưu tiên xếp sự kiện sắp kết thúc lên trước
                .ToListAsync();

            // Trả về danh sách thô (hoặc bọc object tùy anh thích, trả về list cho Frontend dễ map)
            return Ok(ongoingEvents);
        }
        // 1. Lấy danh sách toàn bộ sự kiện (GET: api/events)
        [HttpGet]
        public async Task<IActionResult> GetEvents(int page = 1, int pageSize = 10, string? search = null)
            {
                // Bắt đầu với query toàn bộ dữ liệu
                var query = _context.Events.AsQueryable();

                // 1. Lọc theo trạng thái (Business Logic)
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (userRole != "Admin")
                {
                    query = query.Where(e => e.Status == "Published");
                }

                // 2. Tìm kiếm (Search)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    // Tìm theo Tiêu đề hoặc Địa điểm
                    query = query.Where(e => e.Title.Contains(search) || e.Location.Contains(search));
                }

                // 3. Lấy tổng số lượng để Frontend tính toán trang
                var totalItems = await query.CountAsync();

                // 4. Thực hiện Phân trang (Pagination)
                var events = await query.Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

                // Trả về một object bao gồm metadata và dữ liệu
                return Ok(new
                {
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Data = events
                });
            }
        
        // 2. Lấy thông tin chi tiết MỘT sự kiện (GET: api/events/{id})
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
            {
                var myEvent = await _context.Events.FindAsync(id);

                if (myEvent == null)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện này!" });
                }

                return Ok(myEvent);
            }

            // 3. Tạo mới một sự kiện (POST: api/events)
            [Authorize(Roles = "Admin")]
            [HttpPost]
            public async Task<ActionResult<Event>> CreateEvent(Event myEvent)
            {
                _context.Events.Add(myEvent); // Thêm sự kiện vào bộ nhớ tạm
                await _context.SaveChangesAsync(); // Lưu thật xuống SQL Server

                return Ok(myEvent); // Trả về thông tin sự kiện vừa tạo
            }

            // 4. Cập nhật thông tin sự kiện (PUT: api/events/{id})
            [Authorize(Roles = "Admin")]
            [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, Event updatedEvent)
            {
                // Kiểm tra xem ID trên URL có khớp với ID trong body gửi lên không
                if (id != updatedEvent.Id)
                {
                    return BadRequest(new { message = "ID sự kiện không khớp!" });
                }

                var existingEvent = await _context.Events.FindAsync(id);
                if (existingEvent == null)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện cần sửa!" });
                }

                // Cập nhật các trường thông tin
                existingEvent.Title = updatedEvent.Title;
                existingEvent.Description = updatedEvent.Description;
                existingEvent.Location = updatedEvent.Location;
                existingEvent.StartTime = updatedEvent.StartTime;
                existingEvent.EndTime = updatedEvent.EndTime;
                existingEvent.Address = updatedEvent.Address;
                existingEvent.MaxAttendees = updatedEvent.MaxAttendees;
                existingEvent.BannerUrl = updatedEvent.BannerUrl;
                existingEvent.Status = updatedEvent.Status;

                existingEvent.RegistrationStartTime = updatedEvent.RegistrationStartTime;
                existingEvent.RegistrationEndTime = updatedEvent.RegistrationEndTime;
                existingEvent.TrainingPoints = updatedEvent.TrainingPoints;

                // Không cho phép đổi OrganizerId (Người tạo mặc định không đổi)
                // existingEvent.OrganizerId = updatedEvent.OrganizerId; 

                try
                {
                    await _context.SaveChangesAsync(); // Lưu thay đổi
                }
                catch (DbUpdateConcurrencyException)
                {
                    return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật." });
                }

                return Ok(new { message = "Cập nhật sự kiện thành công!", data = existingEvent });
            }
       
        // 5. Xóa/Hủy một sự kiện (DELETE: api/events/{id})
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
            {
                var myEvent = await _context.Events.FindAsync(id);
                if (myEvent == null)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện cần xóa!" });
                }

                _context.Events.Remove(myEvent); // Xóa khỏi bộ nhớ tạm
                await _context.SaveChangesAsync(); // Áp dụng xuống DB

                return Ok(new { message = "Đã xóa sự kiện thành công!" });
            }
        }
    }