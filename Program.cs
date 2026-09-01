using System.Text;
using HRMS.Infrastructure;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Auth.Services;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

//--------------------------------------------------------------------------------//
// Infrastructure/AppDbContext.cs의 
// 컨트롤러(EquipmentsController, CompressorsController) 활성화
//--------------------------------------------------------------------------------//
builder.Services.AddControllers(); 

//--------------------------------------------------------------------------------//
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//--------------------------------------------------------------------------------//
builder.Services.AddOpenApi();

//--------------------------------------------------------------------------------//
// PostgreSQL DB연결. DbContext를 DI 컨테이너에 등록
// appsettings.Development.json -> appsettings.json의 ConnectionStrings:Default 사용
//--------------------------------------------------------------------------------//
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

//--------------------------------------------------------------------------------//
// 로그인 인증 (JWT Bearer). 
// 키/발급자는 appsettings의 Jwt:Key, Jwt:Issuer 사용 (Modules/Auth 참고)
//--------------------------------------------------------------------------------//
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

//--------------------------------------------------------------------------------//
// 개발 중 프론트(별도 프로젝트, 다른 포트의 개발 서버)에서 이 API를 호출할 수 있도록 하는 CORS.
// 운영 프론트가 정해지면 특정 origin으로 좁혀야 한다.
//--------------------------------------------------------------------------------//
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
}

//--------------------------------------------------------------------------------//
// 압축기 폴링 백그라운드 서비스 
// 앱 시작과 함께 자동으로 돌기 시작한다 (Modules/Communication/CompressorPollingService.cs)
//--------------------------------------------------------------------------------//
builder.Services.AddHostedService<HRMS.Modules.Communication.CompressorPollingService>();

//--------------------------------------------------------------------------------//
// 1분 정각마다 트렌드(DailyTrend)를 기록하는 백그라운드 서비스 
// (Modules/Trend/TrendRecordingService.cs)
//--------------------------------------------------------------------------------//
builder.Services.AddHostedService<HRMS.Modules.Trend.TrendRecordingService>();

//--------------------------------------------------------------------------------//
// Add services to the container.
// 운영 시 Windows Service로 등록 실행, 콘솔 모드 실행도 그대로 지원
//--------------------------------------------------------------------------------//
builder.Host.UseWindowsService(); 

var app = builder.Build();

//--------------------------------------------------------------------------------//
// 최초 실행 시 관리자 계정이 하나도 없으면 부트스트랩용 계정을 자동 생성한다 (admin / admin1234).
// 비밀번호 해시는 PasswordHasher로 런타임에 계산해야 해서 SQL 시드 대신 여기서 처리한다.
// 반드시 최초 로그인 후 비밀번호를 변경할 것 (setup.md 참고).
//--------------------------------------------------------------------------------//
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.Users.AnyAsync())
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Username = "admin",
            FullName = "시스템관리자",
            Role = UserRole.시스템관리자,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "admin1234");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    //--------------------------------------------------------------------------------//
    // 시스템 시작 이벤트 기록(System 카테고리). 화면 없이 백그라운드로 도는 Windows Service라,
    // 나중에 "그때 서버가 정상적으로 떴는지"를 확인할 유일한 흔적이다. 여기까지 온 것 자체가
    // DB 연결이 정상이라는 뜻이라 별도 "DB 연결 확인" 로그는 만들지 않는다.
    // TestMode 여부를 꼭 남기는 이유: 운영에 실수로 켜진 채 배포되면 이 로그로 바로 알아챌 수 있다.
    //--------------------------------------------------------------------------------//
    bool testMode = app.Configuration.GetValue("Communication:TestMode", false);
    int equipmentCount = await db.Equipments.CountAsync();
    int compressorCount = await db.Compressors.CountAsync();
    
    await EventLogger.LogAsync(db, EventLogCategory.System,
        $"HRMS 백엔드 시작 (TestMode={testMode}, 장비 {equipmentCount}대, 압축기 {compressorCount}대)");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
