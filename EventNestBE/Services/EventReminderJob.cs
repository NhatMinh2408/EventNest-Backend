using EventNestBE.Data;
using Microsoft.EntityFrameworkCore;

public class EventReminderJob
{
    private readonly ApplicationDbContext _context; // Đổi lại tên DbContext của anh nếu khác
    private readonly IEmailService _emailService;

    public EventReminderJob(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task ProcessDailyReminders()
    {
        // Tìm sự kiện diễn ra vào "ngày mai"
        var tomorrow = DateTime.Today.AddDays(1);

        var eventsTomorrow = await _context.Events
            .Include(e => e.Registrations) // Tên bảng đăng ký của anh
            .Where(e => e.StartTime.Date == tomorrow.Date && e.Status == "Published")
            .ToListAsync();

        if (!eventsTomorrow.Any()) return;

        foreach (var ev in eventsTomorrow)
        {
            foreach (var reg in ev.Registrations)
            {
                // Kiểm tra null để tránh lỗi nếu dữ liệu bị hỏng
                if (reg.User == null) continue;

                // Lấy thông tin qua object Student
                var studentEmail = reg.User.Email;
                var studentName = reg.User.FullName; // Hoặc Name, FirstName... tuỳ anh thiết kế

                string subject = $"[Nhắc nhở] Sự kiện '{ev.Title}' sẽ diễn ra vào ngày mai!";
                string body = $@"
            <h3>Chào {studentName},</h3>
            <p>Đây là email nhắc nhở tự động từ hệ thống EventNest.</p>
            <p>Sự kiện <strong>{ev.Title}</strong> sẽ diễn ra vào ngày mai.</p>
            <ul>
                <li><strong>Thời gian:</strong> {ev.StartTime:HH:mm} - {ev.StartTime:dd/MM/yyyy}</li>
                <li><strong>Địa điểm:</strong> {ev.Location}</li>
            </ul>
            <p>Vui lòng đến đúng giờ để tiến hành điểm danh nhé!</p>
        ";

                await _emailService.SendEmailAsync(studentEmail, subject, body);
            }
        }
    }
}