namespace EventNestBE.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // --- Đã có Address & Location ---
        public string Address { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        // --- THỜI GIAN SỰ KIỆN DIỄN RA ---
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // --- THỜI GIAN MỞ/ĐÓNG ĐĂNG KÝ (MỚI THÊM) ---
        public DateTime RegistrationStartTime { get; set; }
        public DateTime RegistrationEndTime { get; set; }

        // --- ĐIỂM RÈN LUYỆN (MỚI THÊM) ---
        // Mặc định bằng 0, Admin sẽ nhập khi tạo sự kiện
        public int TrainingPoints { get; set; } = 0;

        public int MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; } = 0;
        public string BannerUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public int OrganizerId { get; set; }
    }
}