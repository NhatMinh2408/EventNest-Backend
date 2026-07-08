using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EventNestBE.Models
{
    public class Registration
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public int StudentId { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public bool IsCheckedIn { get; set; } = false;
        public DateTime? CheckInTime { get; set; }
        public bool Is24hReminderSent { get; set; } = false;
        public bool Is1hReminderSent { get; set; } = false;

        // [QUAN TRỌNG]: Thêm [JsonIgnore] ở đây nếu anh đứng từ Student muốn lấy List Registration mà không bị cuộn ngược Event dài thò lò.
        [JsonIgnore]
        public virtual Event Event { get; set; }

        [ForeignKey("StudentId")]
        public virtual User User { get; set; }
    }
}