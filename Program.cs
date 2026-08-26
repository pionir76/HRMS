using HRMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(); // 컨트롤러(EquipmentsController, CompressorsController) 활성화
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// PostgreSQL 연결. appsettings.Development.json / appsettings.json의 ConnectionStrings:Default 사용
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// 압축기 폴링 백그라운드 서비스 — 앱 시작과 함께 자동으로 돌기 시작한다 (Modules/Communication/CompressorPollingService.cs)
builder.Services.AddHostedService<HRMS.Modules.Communication.CompressorPollingService>();

// Add services to the container.
builder.Host.UseWindowsService(); // 운영 시 Windows Service로 등록 실행, 콘솔 모드 실행도 그대로 지원

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
