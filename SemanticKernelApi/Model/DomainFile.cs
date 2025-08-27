namespace SemanticKernelApi.Model;

public class DomainFile(string fileId, string name, string driveId)
{
    public string FileId { get; } = fileId;
    public string Name { get; } = name;
    public string DriveId { get; } = driveId;
}