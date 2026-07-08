using EventNestBE.Data;
using Microsoft.EntityFrameworkCore;

public class EventReminderJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public EventReminderJob(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // 1. Nhắc trước 1 NGÀY
    public async Task ProcessDailyReminders()
    {
        var now = DateTime.UtcNow;
        var tomorrowStart = now.Date.AddDays(1);
        var tomorrowEnd = tomorrowStart.AddDays(1).AddTicks(-1);

        // Chỉ kéo lên những đơn đăng ký CHƯA gửi nhắc nhở 24h
        var eventsTomorrow = await _context.Events
            .Include(e => e.Registrations.Where(r => !r.Is24hReminderSent))
                .ThenInclude(r => r.User)
            .Where(e => e.StartTime >= tomorrowStart && e.StartTime <= tomorrowEnd && e.Status == "Published")
            .ToListAsync();

        foreach (var ev in eventsTomorrow)
        {
            foreach (var reg in ev.Registrations)
            {
                if (reg.User == null || string.IsNullOrEmpty(reg.User.Email)) continue;

                // Gửi mail
                string subject = $"[Nhắc nhở] Sự kiện '{ev.Title}' sẽ diễn ra vào ngày mai!";
                string body = $"<h3>Chào {reg.User.FullName},</h3><p>Sự kiện <strong>{ev.Title}</strong> sẽ diễn ra vào ngày mai tại {ev.Location}.</p>";

                await _emailService.SendEmailAsync(reg.User.Email, subject, body);

                // Gắn cờ đã gửi thành công
                reg.Is24hReminderSent = true;
            }
            // Lưu sau mỗi sự kiện để tránh mất dữ liệu nếu có lỗi giữa chừng
            await _context.SaveChangesAsync();
        }
    }

    // 2. Nhắc trước 1 GIỜ
    public async Task ProcessHourlyReminders()
    {
        var now = DateTime.UtcNow;
        var oneHourLaterStart = now.AddMinutes(50); // Quét khoảng biên an toàn 50-70 phút
        var oneHourLaterEnd = now.AddMinutes(70);

        // Chỉ kéo lên những đơn đăng ký CHƯA gửi nhắc nhở 1h
        var eventsNextHour = await _context.Events
            .Include(e => e.Registrations.Where(r => !r.Is1hReminderSent))
                .ThenInclude(r => r.User)
            .Where(e => e.StartTime >= oneHourLaterStart && e.StartTime <= oneHourLaterEnd && e.Status == "Published")
            .ToListAsync();

        foreach (var ev in eventsNextHour)
        {
            foreach (var reg in ev.Registrations)
            {
                if (reg.User == null || string.IsNullOrEmpty(reg.User.Email)) continue;

                // Gửi mail
                string subject = $"[Khẩn] Sự kiện '{ev.Title}' sắp diễn ra trong 1 giờ tới!";
                string body = $"<h3>Chào {reg.User.FullName},</h3><p>Sự kiện <strong>{ev.Title}</strong> sắp bắt đầu, bạn hãy di chuyển đến {ev.Location} nhé.</p>";

                await _emailService.SendEmailAsync(reg.User.Email, subject, body);

                // Gắn cờ đã gửi thành công
                reg.Is1hReminderSent = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}