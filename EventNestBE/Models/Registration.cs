using System.ComponentModel.DataAnnotations.Schema; // Đừng quên import thư viện này

namespace EventNestBE.Models
{
    public class Registration
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        // Khóa ngoại cho User
        public int StudentId { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public bool IsCheckedIn { get; set; } = false;

        public DateTime? CheckInTime { get; set; }

        public virtual Event Event { get; set; }

        // Cấu hình rõ ràng mối quan hệ ở đây
        [ForeignKey("StudentId")]
        public virtual User User { get; set; }
    }
}