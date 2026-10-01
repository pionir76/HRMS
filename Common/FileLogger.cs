using System.Collections.Concurrent;
using System.Text;

namespace HRMS.Common;

//--------------------------------------------------------------------------------//
// 하루 한 개씩 파일에 남기는 최소 로거(2026-09-28 추가).
//
// 왜 필요한가: 이 앱은 Windows 서비스로 돌기 때문에 콘솔 출력이 보이지 않는다.
// UseWindowsService()가 Windows 이벤트 로그 provider를 붙여줘서 Warning 이상은 이벤트
// 뷰어에 남지만, 현장에서 "지난주 그 시각에 무슨 일이 있었나"를 훑기에는 불편하고
// 보관 용량이 차면 오래된 항목부터 지워진다. 그래서 파일로도 같이 남긴다.
//
// 외부 로깅 패키지(Serilog 등)를 쓰지 않은 이유: 폐쇄망이라 패키지 추가가 번거롭고,
// 이 정도 규모에는 파일 한 줄 append로 충분하다(overview.md의 "단순함 우선" 원칙).
//
// 위치: {실행파일 폴더}/App_Data/logs/hrms-yyyyMMdd.log — 첨부파일과 같은 기준으로
// AppContext.BaseDirectory를 쓴다(서비스는 작업 디렉토리가 달라질 수 있다).
// 보존: 기본 30일, Logging:File:RetentionDays로 조정.
//--------------------------------------------------------------------------------//
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string directory;
    private readonly int retentionDays;
    private readonly ConcurrentDictionary<string, FileLogger> loggers = new();
    private readonly Lock writeLock = new();
    private DateOnly lastCleanupDate;

    public FileLoggerProvider(string directory, int retentionDays)
    {
        this.directory = Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(AppContext.BaseDirectory, directory);
        this.retentionDays = retentionDays;
        Directory.CreateDirectory(this.directory);
    }

    public ILogger CreateLogger(string categoryName) =>
        loggers.GetOrAdd(categoryName, name => new FileLogger(this, name));

    public void Dispose() => loggers.Clear();

    //--------------------------------------------------------------------------------//
    // 한 줄 append. 로깅 실패가 앱을 멈추면 안 되므로 예외는 전부 삼킨다(디스크가 꽉 찬
    // 상황에서도 수집은 계속되어야 한다).
    //--------------------------------------------------------------------------------//
    internal void Write(string line)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            string path = Path.Combine(directory, $"hrms-{today:yyyyMMdd}.log");

            lock (writeLock)
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);

                if (lastCleanupDate != today)
                {
                    lastCleanupDate = today;
                    DeleteOldFiles(today);
                }
            }
        }
        catch
        {
            // 로그를 못 남기는 것보다 앱이 멈추는 쪽이 더 나쁘다.
        }
    }

    private void DeleteOldFiles(DateOnly today)
    {
        var limit = today.AddDays(-retentionDays);
        foreach (string file in Directory.GetFiles(directory, "hrms-*.log"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (DateOnly.TryParseExact(name["hrms-".Length..], "yyyyMMdd", out var date) && date < limit)
                File.Delete(file);
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        // 레벨 필터링은 ILoggerFactory가 appsettings의 Logging:LogLevel 설정대로 처리한다.
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var line = new StringBuilder()
                .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(" [").Append(Level(logLevel)).Append("] ")
                .Append(categoryName).Append(" - ")
                .Append(formatter(state, exception));

            if (exception is not null)
                line.Append(Environment.NewLine).Append(exception);

            provider.Write(line.ToString());
        }

        private static string Level(LogLevel level) => level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };
    }
}
