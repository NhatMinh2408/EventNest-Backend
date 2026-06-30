namespace EventNestBE.Models
{
    public class Registration
    {
        public int Id { get; set; }

        // Liên kết tới ID của Sự kiện
        public int EventId { get; set; }

        // Liên kết tới ID của Sinh viên (User)
        public int StudentId { get; set; }

        // Thời gian bấm nút đăng ký (mặc định lấy giờ hiện tại)
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Trạng thái điểm danh: true (đã đi), false (chưa đi)
        public bool IsCheckedIn { get; set; } = false;

        // Thời gian quét mã check-in (dấu ? nghĩa là có thể để trống nếu chưa đi)
        public DateTime? CheckInTime { get; set; }
    }
}