namespace HRMS.Modules.Attachment;

//-----------------------------------------------------------------------------//
// 첨부파일 실제 바이트를 파일시스템에 저장/조회/삭제한다. 저장 루트는
// appsettings의 FileStorage:RootPath(없으면 실행 파일 옆 App_Data/attachments)다.
// DB에는 이 루트 기준 상대경로만 남긴다(Attachment.StoredPath) — 서버 이동/배포 시
// 루트 경로만 바꾸면 되게 하기 위함이다.
//-----------------------------------------------------------------------------//
public class AttachmentStorage(IConfiguration configuration)
{
    // 상대경로로 설정된 경우 실행 파일 기준으로 고정한다 — Windows 서비스로 돌 때는
    // 현재 작업 디렉토리가 실행 파일 위치와 다를 수 있어서, Path.Combine 기본 동작(현재 작업
    // 디렉토리 기준)에 맡기면 배포 방식에 따라 저장 위치가 달라지는 문제가 생긴다.
    private readonly string _rootPath = Path.IsPathRooted(configuration["FileStorage:RootPath"])
        ? configuration["FileStorage:RootPath"]!
        : Path.Combine(AppContext.BaseDirectory, configuration["FileStorage:RootPath"] ?? Path.Combine("App_Data", "attachments"));

    public async Task<string> SaveAsync(string ownerTypeFolder, string extension, Stream content)
    {
        var dir = Path.Combine(_rootPath, ownerTypeFolder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(dir, fileName);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream);
        return Path.Combine(ownerTypeFolder, fileName);
    }

    public string GetFullPath(string relativePath) => Path.Combine(_rootPath, relativePath);

    public void Delete(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
