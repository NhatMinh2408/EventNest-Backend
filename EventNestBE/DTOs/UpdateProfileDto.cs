using System.ComponentModel.DataAnnotations;

namespace EventNestBE.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress] // Tự động validate định dạng email
        public string Email { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Cohort { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        // Chú ý: Không có MSSV, Role, hay TrainingPoints ở đây
    }
}