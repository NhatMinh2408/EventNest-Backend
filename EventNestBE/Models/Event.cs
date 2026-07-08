using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EventNestBE.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public DateTime RegistrationStartTime { get; set; }
        public DateTime RegistrationEndTime { get; set; }

        public int TrainingPoints { get; set; } = 0;
        public int PenaltyPoints { get; set; } = 0;

        public int MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; } = 0;
        public string BannerUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public int OrganizerId { get; set; }

        // [QUAN TRỌNG]: Thêm [JsonIgnore] để khi API trả JSON không bị lặp vô tận
        [JsonIgnore]
        public virtual ICollection<Registration>? Registrations { get; set; }
    }
}