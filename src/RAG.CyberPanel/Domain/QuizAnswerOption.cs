namespace RAG.CyberPanel.Domain;

public class QuizAnswerOption
{
    public Guid QuizAnswerId { get; set; }
    public QuizAnswer? QuizAnswer { get; set; }
    public Guid OptionId { get; set; }
    public Option? Option { get; set; }
}
