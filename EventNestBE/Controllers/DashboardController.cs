using EventNestBE.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventNestBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xem thống kê
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // 1. Tổng số sinh viên (Role = "Student")
            var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");

            // 2. Tổng số sự kiện
            var totalEvents = await _context.Events.CountAsync();

            // 3. Tổng số lượt đăng ký
            var totalRegistrations = await _context.Registrations.CountAsync();

            // 4. Tổng số lượt đã check-in
            var totalCheckIns = await _context.Registrations.CountAsync(r => r.IsCheckedIn);

            // 5. Tính tỷ lệ tham gia (Attendance Rate)
            double attendanceRate = 0;
            if (totalRegistrations > 0)
            {
                attendanceRate = (double)totalCheckIns / totalRegistrations * 100;
            }

            return Ok(new
            {
                TotalStudents = totalStudents,
                TotalEvents = totalEvents,
                TotalRegistrations = totalRegistrations,
                TotalCheckIns = totalCheckIns,
                AttendanceRatePercentage = Math.Round(attendanceRate, 2) // Làm tròn 2 chữ số thập phân
            });
        }
    }
}