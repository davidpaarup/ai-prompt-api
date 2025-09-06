namespace AiPromptApi.Config;

public static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        
        if (value == null)
        {
            throw new Exception();
        } 
        return value;
    }
    
    public static T GetRequiredSection<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetRequiredSection(key).Get<T>();
        
        if (value == null)
        {
            throw new Exception();
        } 
        
        return value;
    }
}
