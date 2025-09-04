using System.ComponentModel;
using AiPromptApi.Model;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class OneDrivePlugin
{
    private readonly GraphClient _graphClient;
        
    public OneDrivePlugin(GraphClientFactory graphClientFactory)
    {
        IEnumerable<string> scopes = ["files.read", "files.read.all"];
        _graphClient = graphClientFactory.Create(scopes);
    }

    [KernelFunction("fetch_file_names_and_ids_on_root")]
    [Description("Fetches the file names and IDs in a Drive root")]
    private Task<IEnumerable<DomainFile>> FetchNumberOfFilesAsync()
    {
        return _graphClient.GetOneDriveItemsAsync();
    }
    
    [KernelFunction("fetch_content_of_file")]
    [Description("Fetches the content of a file, given the file ID")]
    private Task<string> FetchNumberOfFilesAsync(string fileId)
    {
        return _graphClient.GetFileContentAsync(fileId);
    }
}