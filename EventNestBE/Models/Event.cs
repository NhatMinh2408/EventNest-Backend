namespace EventNestBE.Models
{
    public class Event
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty; // Tên sự kiện

        public string Description { get; set; } = string.Empty; // Mô tả chi tiết

        public string Location { get; set; } = string.Empty; // Địa điểm diễn ra

        public DateTime StartTime { get; set; } // Thời gian bắt đầu

        public DateTime EndTime { get; set; } // Thời gian kết thúc

        public int MaxAttendees { get; set; } // Số lượng sinh viên tối đa được đăng ký

        public string BannerUrl { get; set; } = string.Empty; // Ảnh nền sự kiện

        // Trạng thái sự kiện: Draft (Nháp), Published (Đã đăng), Completed (Đã kết thúc)
        public string Status { get; set; } = "Draft";

        // ID của người tạo sự kiện (Liên kết với bảng User ở trên)
        public int OrganizerId { get; set; }
    }
}