using AiPromptApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiPromptApi.Controllers;

[Route("/[controller]")]
[ApiController]
public class PromptController(IKernelService kernelService) : ControllerBase
{
    [Authorize]
    public async Task<IActionResult> Post([FromBody] PromptInput payload)
    {
        var stream = kernelService.GetReplyAsync(payload.Message);

        await foreach (var chunk in stream)
        {
            await HttpContext.Response.WriteAsync(chunk);
            await HttpContext.Response.Body.FlushAsync();
        }

        return new EmptyResult();
    }
}

public class PromptInput
{
    public string Message { get; set; } = string.Empty;
}