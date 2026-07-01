namespace RAG.Orchestrator.Api.Services;

public class LlmServiceUnavailableException : Exception
{
    public LlmServiceUnavailableException(string message)
        : base(message)
    {
    }

    public LlmServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
