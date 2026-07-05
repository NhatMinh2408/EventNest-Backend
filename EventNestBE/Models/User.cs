namespace EventNestBE.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // --- THÊM TRƯỜNG MSSV RIÊNG ---
        public string Mssv { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public int TrainingPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- CÁC TRƯỜNG CHO TRANG PROFILE ---
        public string AvatarUrl { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Cohort { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
    }
}