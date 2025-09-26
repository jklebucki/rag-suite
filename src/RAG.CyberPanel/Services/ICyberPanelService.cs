using RAG.CyberPanel.Domain;

namespace RAG.CyberPanel.Services;

public interface ICyberPanelService
{
    int CalculateScore(Quiz quiz, IEnumerable<QuizAnswer> answers);
}
