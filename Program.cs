using System.Text;
using HRMS.Common;
using Microsoft.AspNetCore.Diagnostics;
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
//--------------------------------------------------------------------------------//
// 필수 설정 검증(2026-09-28 추가). 값이 없으면 여기서 원인을 분명히 밝히고 멈춘다.
// 그 전에는 운영 환경(appsettings.json에 두 값이 없음)에서 띄우면 한참 뒤 DB 첫 조회에서
// "The ConnectionString property has not been initialized" 스택 트레이스만 남기고 죽었다.
// Windows 서비스는 콘솔이 없어 그 메시지도 안 보이고 "시작했다가 즉시 중지됨"으로만 보인다.
// 환경변수로 주는 경우 이름은 ConnectionStrings__Default / Jwt__Key 다(구분자가 밑줄 2개).
//--------------------------------------------------------------------------------//
string connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "DB 연결 문자열이 없습니다. appsettings.json의 ConnectionStrings:Default 또는 " +
        "환경변수 ConnectionStrings__Default를 설정하세요 (Doc/setup.md 8장).");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "DB 연결 문자열이 비어 있습니다. appsettings.json의 ConnectionStrings:Default를 채우세요 (Doc/setup.md 8장).");

string jwtKey = builder.Configuration["Jwt:Key"] ?? "";
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT 서명 키가 없습니다. appsettings.json의 Jwt:Key 또는 환경변수 Jwt__Key를 설정하세요 (Doc/setup.md 8장).");

//--------------------------------------------------------------------------------//
// HMAC-SHA256은 키가 256비트(32바이트) 이상이어야 한다. 짧으면 앱은 정상 기동하고
// 로그인 시점에야 IDX10653으로 전 사용자가 로그인 불가가 되므로 여기서 미리 막는다.
//--------------------------------------------------------------------------------//
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        $"JWT 서명 키가 너무 짧습니다(현재 {Encoding.UTF8.GetByteCount(jwtKey)}바이트). 32바이트 이상이어야 합니다 (Doc/setup.md 8장).");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

//--------------------------------------------------------------------------------//
// 파일 로그(2026-09-28 추가). Windows 서비스로 돌면 콘솔이 안 보이고, 이벤트 뷰어는
// Warning 이상만 남는 데다 현장에서 기간별로 훑기 불편하다. 실행 파일 옆
// App_Data/logs/hrms-yyyyMMdd.log에 같이 남긴다(기본 30일 보관, Common/FileLogger.cs).
// 레벨은 appsettings의 Logging:LogLevel 설정을 그대로 따른다.
//--------------------------------------------------------------------------------//
builder.Logging.AddProvider(new FileLoggerProvider(
    builder.Configuration.GetValue("Logging:File:Directory", "App_Data/logs")!,
    builder.Configuration.GetValue("Logging:File:RetentionDays", 30)));

//--------------------------------------------------------------------------------//
// 로그인 인증 (JWT Bearer). 
// 키/발급자는 appsettings의 Jwt:Key, Jwt:Issuer 사용 (Modules/Auth 참고)
//--------------------------------------------------------------------------------//
builder.Services.AddScoped<JwtTokenService>();

//--------------------------------------------------------------------------------//
// 첨부파일(장비 사진 등) 저장 헬퍼. 실제 바이트는 DB가 아니라 파일시스템에 저장한다
// (appsettings의 FileStorage:RootPath, Modules/Attachment/README.md 참고).
//--------------------------------------------------------------------------------//
builder.Services.AddScoped<HRMS.Modules.Attachment.AttachmentStorage>();

//--------------------------------------------------------------------------------//
// 비상정지 처리(D1805 쓰기). 이 시스템에서 장비에 쓰기를 하는 유일한 동작이다
// (Modules/EmergencyStop/README.md 참고).
//--------------------------------------------------------------------------------//
builder.Services.AddScoped<HRMS.Modules.EmergencyStop.Services.EmergencyStopService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

//--------------------------------------------------------------------------------//
// CORS. 브라우저는 포트만 달라도 다른 origin으로 보기 때문에, 프론트를 API와 다른 포트에서
// 서비스하면 운영에서도 CORS 등록이 필요하다(폐쇄망 여부와 무관한 브라우저 규칙).
//   - 개발: 모든 origin 허용(프론트 개발 서버 포트가 자주 바뀜)
//   - 운영: appsettings의 Cors:AllowedOrigins에 적힌 origin만 허용. 비워두면 CORS를 아예 켜지
//     않는다 — 프론트를 API와 같은 주소·포트에서 서빙하는 경우가 여기 해당한다.
//--------------------------------------------------------------------------------//
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
bool useCors = builder.Environment.IsDevelopment() || allowedOrigins.Length > 0;

if (useCors)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(allowedOrigins);

        policy.AllowAnyMethod().AllowAnyHeader();
    }));
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
// 점검일지 자동 기록 서비스 — 매일 한국시간 00:00에 전날 값을 이어서 채운다
// (Modules/Inspection/InspectionAutoFillService.cs)
//--------------------------------------------------------------------------------//
builder.Services.AddHostedService<HRMS.Modules.InspectionReport.InspectionAutoFillService>();

//--------------------------------------------------------------------------------//
// 운전일지 자동 기록 서비스 — 매일 한국시간 09/13/16/21시에 채널 자동측정 항목을 덮어쓰고,
// 사용자 직접입력 항목·기준값은 이어채운다 (Modules/OperationReport/OperationAutoFillService.cs)
//--------------------------------------------------------------------------------//
builder.Services.AddHostedService<HRMS.Modules.OperationReport.OperationAutoFillService>();

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

    //--------------------------------------------------------------------------------//
    // HTTPS 리다이렉트는 개발에서만 건다(2026-09-28). 운영은 폐쇄망 HTTP로 서비스하며
    // Kestrel HTTPS 엔드포인트/인증서 설정이 없어서, 그대로 두면 리다이렉트 대상 포트를
    // 못 찾아 경고만 남기거나 프론트 요청이 엉뚱하게 실패할 수 있다.
    //--------------------------------------------------------------------------------//
    app.UseHttpsRedirection();
}

//--------------------------------------------------------------------------------//
// 전역 예외 처리(2026-09-28 추가). 그 전에는 API에서 예외가 나면 본문 없는 500만 나가서,
// 프론트는 원인을 알 수 없고 서버 로그와 대조할 추적 번호도 없었다.
// 이제 같은 traceId를 응답과 로그 양쪽에 남겨서, 사용자가 화면의 번호를 알려주면
// 로그에서 바로 찾을 수 있다.
//--------------------------------------------------------------------------------//
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HRMS.UnhandledException");
    logger.LogError(error, "처리되지 않은 예외 (traceId: {TraceId}, {Method} {Path})",
        context.TraceIdentifier, context.Request.Method, context.Request.Path);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsJsonAsync(new
    {
        traceId = context.TraceIdentifier,
        message = "서버 내부 오류가 발생했습니다. 관리자에게 이 추적 번호를 알려주세요."
    });
}));

if (useCors)
    app.UseCors();

//--------------------------------------------------------------------------------//
// 프론트 화면 파일 제공(2026-10-02 추가). 현장 서버는 http://서버주소/ (80번 포트) 하나로
// 화면과 API를 함께 내보낸다. 프론트 빌드 결과(index.html, js, css …)를 실행 파일 옆
// wwwroot 폴더에 넣으면 여기서 그대로 내려준다 — 별도 웹 서버(IIS 등)가 필요 없고,
// 화면과 API가 같은 주소라 CORS 설정도 필요 없다(Doc/setup.md 8장).
//
// wwwroot가 없으면(개발 PC처럼 프론트를 따로 띄우는 경우) 아무 일도 하지 않는다.
//--------------------------------------------------------------------------------//
app.UseDefaultFiles();
app.UseStaticFiles();

//--------------------------------------------------------------------------------//
// 헬스체크(2026-09-28 추가). 인증 없이 호출할 수 있는 유일한 엔드포인트다 — 외부 감시
// 스크립트나 Windows 서비스 복구 정책이 "프로세스는 살아있는데 DB가 죽은" 상태를 감지할
// 수단이 필요하기 때문이다(폐쇄망이라 노출 부담은 낮다).
// 상세 상태는 로그인이 필요한 GET /api/system/status(api-manual 71번)를 쓴다.
//--------------------------------------------------------------------------------//
app.MapGet("/health", async (AppDbContext db) =>
{
    bool dbOk;
    try { dbOk = await db.Database.CanConnectAsync(); }
    catch { dbOk = false; }

    var lastPoll = HRMS.Modules.Communication.CompressorPollingService.LastCycleCompletedAt;
    bool pollingOk = lastPoll is { } at && DateTimeOffset.UtcNow - at < TimeSpan.FromSeconds(30);

    var body = new
    {
        status = dbOk && pollingOk ? "정상" : "오류",
        database = dbOk ? "정상" : "연결 실패",
        lastPollAt = lastPoll,
        polling = pollingOk ? "정상" : "지연 또는 중단"
    };

    return dbOk && pollingOk ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//--------------------------------------------------------------------------------//
// 화면 주소를 새로고침하거나 직접 입력했을 때(예: /equipments/12) 파일이 없어도 index.html을
// 돌려줘서 프론트가 그 화면을 그리게 한다(SPA 방식). 두 가지는 제외한다:
//   - nonfile: 확장자가 있는 경로(.js/.css/.png …). 이 조건이 없으면 실제 파일 요청까지
//     여기에 잡혀 JS·CSS 대신 index.html이 나가 화면이 깨진다(2026-10-02 확인). 없는 파일은 404.
//   - /api/로 시작하는 주소: 없는 API를 부르면 화면 HTML이 아니라 404가 가야 프론트가
//     오류를 알아챌 수 있다.
//--------------------------------------------------------------------------------//
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/).*$)}", "index.html");

app.Run();
