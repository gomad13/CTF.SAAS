namespace CTF.Api.Services;

public class ChatbotFactory
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;

    public ChatbotFactory(IConfiguration config, IServiceProvider services)
    {
        _config = config;
        _services = services;
    }

    public IChatbotService GetService()
    {
        var provider = (_config["Chatbot:Provider"] ?? "ollama").ToLowerInvariant();
        return provider switch
        {
            "ollama" => _services.GetRequiredService<OllamaChatbotService>(),
            _ => _services.GetRequiredService<OllamaChatbotService>(),
        };
    }
}
