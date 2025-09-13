using System.ComponentModel;
using AiPromptApi.Model;
using AiPromptApi.Services;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins.Microsoft;

public class MicrosoftOneDrivePlugin(GraphClientFactory graphClientFactory)
{
    [KernelFunction("fetch_file_names_and_ids_on_root")]
    [Description("Fetches the file names and IDs in a Microsoft OneDrive root")]
    private async Task<IEnumerable<DomainFile>> FetchNumberOfFilesAsync()
    {
        var client = await graphClientFactory.CreateAsync();
        
        var drive = await client.Me
            .Drive.GetAsync();
        
        if (drive == null)
        {
            throw new Exception();
        }
        
        var items = await client.Drives[drive.Id]
            .Items["root"].Children.GetAsync();

        if (items?.Value == null)
        {
            throw new Exception();
        }
        
        var result = items.Value.Select(v =>
        {
            if (v.Id == null || v.Name == null)
            {
                throw new Exception();
            }
            
            return new DomainFile(v.Id, v.Name);
        });

        return result;
    }
    
    [KernelFunction("fetch_content_of_file")]
    [Description("Fetches the content of a file, given the file ID, from the Microsoft OneDrive.")]
    private async Task<string> FetchNumberOfFilesAsync(string fileId)
    {
        var split = fileId.Split('!');
        var driveId = split[0];

        var client = await graphClientFactory.CreateAsync();
        var content = await client.Drives[driveId].Items[fileId].Content.GetAsync();

        if (content == null)
        {
            throw new Exception();
        }

        using var reader = new StreamReader(content);
        return await reader.ReadToEndAsync();
    }
}