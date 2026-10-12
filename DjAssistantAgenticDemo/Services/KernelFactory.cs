using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace DjAssistantAgenticDemo.Services;

public sealed class KernelFactory
{
    private readonly IConfiguration _config;
    public KernelFactory(IConfiguration config)
    {
        _config = config;
    }
    public Kernel CreateKernel()
    {
        var endpoint = _config["AZURE_OPENAI_ENDPOINT"];
        var apiKey = _config["AZURE_OPENAI_API_KEY"];
        var deployment = _config["AZURE_OPENAI_CHAT_DEPLOYMENT"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException(
                "Missing Azure OpenAI config. Set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_CHAT_DEPLOYMENT.");
        }

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(deploymentName: deployment, endpoint: endpoint, apiKey: apiKey);
        return builder.Build();
    }
}
