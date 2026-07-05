namespace EventNestBE.Models
{
    public class Registration
    {
        public int Id { get; set; }

        // Liên kết tới ID của Sự kiện (Khóa ngoại - Foreign Key)
        public int EventId { get; set; }

        // Liên kết tới ID của Sinh viên (Khóa ngoại - Foreign Key)
        public int StudentId { get; set; }

        // Thời gian bấm nút đăng ký
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Trạng thái điểm danh
        public bool IsCheckedIn { get; set; } = false;

        // Thời gian quét mã check-in
        public DateTime? CheckInTime { get; set; }

     
        // Thuộc tính này giúp Entity Framework biết Registration này thuộc về Event nào
        public virtual Event Event { get; set; }

        // (Tùy chọn) Nếu bạn muốn lấy thông tin của User/Student thì thêm dòng này
        // public virtual User Student { get; set; } 
    }
}