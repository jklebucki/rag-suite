namespace RAG.DocumentProcessing.Abstractions;

public sealed class DocumentProcessingException : Exception
{
    public DocumentProcessingException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
