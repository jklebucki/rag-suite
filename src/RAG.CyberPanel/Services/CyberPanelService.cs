using RAG.CyberPanel.Domain;

namespace RAG.CyberPanel.Services;

public class CyberPanelService : ICyberPanelService
{
    // Exact-match scoring: award question points only if selected options equal the correct set
    public int CalculateScore(Quiz quiz, IEnumerable<QuizAnswer> answers)
    {
        var questionMap = quiz.Questions.ToDictionary(q => q.Id, q => q);
        int total = 0;

        foreach (var answer in answers)
        {
            if (!questionMap.TryGetValue(answer.QuestionId, out var question))
                continue; // invalid question id -> skip

            var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(x => x).ToArray();
            var selectedOptionIds = answer.SelectedOptions.Select(s => s.OptionId).Distinct().OrderBy(x => x).ToArray();

            if (correctOptionIds.SequenceEqual(selectedOptionIds))
            {
                total += question.Points;
            }
        }

        return total;
    }
}
