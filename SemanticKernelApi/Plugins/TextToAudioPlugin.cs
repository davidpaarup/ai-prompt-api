using System.ComponentModel;
using Microsoft.SemanticKernel;
using ConfigCat.Client;

namespace SemanticKernelApi.Plugins;

public class TextToAudioPlugin(IConfiguration configuration)
{
    [KernelFunction("convert_text_to_audio")]
    [Description("Converts the provided text to an audio and returns the bytes")]
    private async Task<byte[]> ConvertTextToAudioAsync()
    {
        var key = configuration["ConfigCatKey"];

        if (key == null)
        {
            throw new Exception("Config cat configuration is missing");
        }
        
        var client = ConfigCatClient.Get(key);
        await client.ForceRefreshAsync();
        
        var isTextToAudioEnabled = await client.GetValueAsync("isTextToAudioEnabled", false);

        if (!isTextToAudioEnabled)
        {
            throw new Exception("Text to audio conversion is disabled");
        }

        const string response = "This is some audio";
        return System.Text.Encoding.UTF8.GetBytes(response);
    }
}