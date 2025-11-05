namespace RAG.Orchestrator.Api.Common.Constants;

/// <summary>
/// Constants for localization resource keys
/// </summary>
public static class LocalizationKeys
{
    /// <summary>
    /// System prompts section keys
    /// </summary>
    public static class SystemPrompts
    {
        public const string RagAssistant = "rag_assistant";
        public const string RagAssistantNoDocs = "rag_assistant_no_docs";
        public const string ContextInstruction = "context_instruction";
        public const string ContextInstructionNoDocs = "context_instruction_no_docs";
        public const string KnowledgeBaseContext = "knowledge_base_context";
        public const string NoDocumentSearchNote = "no_document_search_note";
        public const string ConversationHistory = "conversation_history";
        public const string CurrentQuestion = "current_question";
        public const string Response = "response";
        public const string DocumentSourceIntro = "document_source_intro";
        public const string ReconstructedFromChunks = "reconstructed_from_chunks";
        public const string TranslationNote = "translation_note";
        public const string DocumentsUnavailable = "documents_unavailable";
    }

    /// <summary>
    /// Instructions section keys
    /// </summary>
    public static class Instructions
    {
        public const string RespondInLanguage = "respond_in_language";
        public const string UseKnowledgeBase = "use_knowledge_base";
        public const string UseGeneralKnowledge = "use_general_knowledge";
        public const string ConsiderHistory = "consider_history";
        public const string BeHonest = "be_honest";
        public const string BeHonestNoDocs = "be_honest_no_docs";
        public const string BeHelpful = "be_helpful";
    }

    /// <summary>
    /// UI labels section keys
    /// </summary>
    public static class UiLabels
    {
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string Document = "document";
        public const string Type = "type";
        public const string Path = "path";
        public const string Score = "score";
        public const string Highlights = "highlights";
        public const string FullContent = "full_content";
        public const string Source = "source";
        public const string Sources = "sources";
        public const string Used = "used";
        public const string Instructions = "instructions";
    }

    /// <summary>
    /// Session labels section keys
    /// </summary>
    public static class SessionLabels
    {
        public const string NewConversation = "new_conversation";
    }

    /// <summary>
    /// Error messages section keys
    /// </summary>
    public static class ErrorMessages
    {
        public const string GenerationFailed = "generation_failed";
        public const string ProcessingError = "processing_error";
    }
}

