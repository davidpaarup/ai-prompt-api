namespace SemanticKernelApi.Model;

public class DomainFile(string fileId, string name)
{
    public string FileId { get; } = fileId;
    public string Name { get; } = name;
}