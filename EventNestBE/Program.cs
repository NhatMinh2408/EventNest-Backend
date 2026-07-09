using EventNestBE.Data;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Auth & JWT ---
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// --- Services & Hangfire ---
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<EventReminderJob>();
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger Config ---
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Token: Bearer <token>"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- Pipeline Config ---
// ĐÃ SỬA: Đưa Swagger ra ngoài điều kiện IsDevelopment() 
// Để khi lên Railway anh vẫn truy cập được trang giao diện API nhằm kiểm tra kết nối dễ dàng hơn.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

app.UseStaticFiles();
app.MapControllers();

// --- Hangfire Jobs (ĐÃ SỬA: Tương thích cả Windows và Linux) ---
TimeZoneInfo vnTimeZone;
try
{
    // Thử lấy múi giờ theo định dạng của hệ điều hành Windows (Chạy ở máy Local)
    vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
}
catch (TimeZoneNotFoundException)
{
    // Nếu không tìm thấy (Khi chạy trên Docker Linux của Railway), sẽ tự động đổi sang định dạng Linux
    vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
}

// Đăng ký các tiến trình chạy ngầm với múi giờ đã cấu hình động
RecurringJob.AddOrUpdate<EventReminderJob>(
    "daily-event-reminder",
    job => job.ProcessDailyReminders(),
    "0 8 * * *",
    new RecurringJobOptions { TimeZone = vnTimeZone });

RecurringJob.AddOrUpdate<EventReminderJob>(
    "hourly-event-reminder",
    job => job.ProcessHourlyReminders(),
    "*/15 * * * *",
    new RecurringJobOptions { TimeZone = vnTimeZone });

app.Run();