using EventNestBE.Data;
using EventNestBE.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ĐÃ SỬA: Dùng UseNpgsql cho PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Lệnh này tương đương với dotnet ef database update
    dbContext.Database.Migrate();
    var adminExists = await dbContext.Users.AnyAsync(u => u.Role == "Admin");
    if (!adminExists)
    {
        var adminUser = new User
        {
            Username = "admin",
            // Lưu ý: Phải hash mật khẩu bằng chính thuật toán anh đang dùng trong hệ thống
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "Administrator",
            Email = "admin@eventnest.com",
            Role = "Admin",
            Mssv = "ADMIN001",
            Faculty = "System",
            Cohort = "N/A"
        };
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }
}

// --- Pipeline Config ---
// ĐỂ RA NGOÀI IsDevelopment() ĐỂ XEM ĐƯỢC SWAGGER TRÊN RAILWAY
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");
app.UseStaticFiles();
app.MapControllers();

// --- Hangfire Jobs ---
TimeZoneInfo vnTimeZone;
try
{
    vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
}
catch (TimeZoneNotFoundException)
{
    vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
}

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