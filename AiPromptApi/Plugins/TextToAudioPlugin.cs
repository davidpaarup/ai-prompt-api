using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class TextToAudioPlugin()
{
    [KernelFunction("convert_text_to_audio")]
    [Description("Converts the provided text to an audio and returns the bytes")]
    private byte[] ConvertTextToAudioAsync()
    {
        throw new Exception("Text to audio conversion not implemented.");
    }
}