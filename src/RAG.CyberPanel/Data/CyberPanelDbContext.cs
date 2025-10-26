using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Domain;

namespace RAG.CyberPanel.Data;

public class CyberPanelDbContext : DbContext
{
    public CyberPanelDbContext(DbContextOptions<CyberPanelDbContext> options) : base(options)
    {
    }

    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Option> Options { get; set; } = null!;
    public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
    public DbSet<QuizAnswer> QuizAnswers { get; set; } = null!;
    public DbSet<QuizAnswerOption> QuizAnswerOptions { get; set; } = null!;
    public DbSet<QuizDeletionLog> QuizDeletionLogs { get; set; } = null!;
    public DbSet<AttemptDeletionLog> AttemptDeletionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Quiz>(b =>
        {
            b.HasKey(q => q.Id);
            b.HasMany(q => q.Questions).WithOne(qn => qn.Quiz!).HasForeignKey(qn => qn.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(b =>
        {
            b.HasKey(q => q.Id);
            b.HasMany(q => q.Options).WithOne(o => o.Question!).HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Option>(b => { b.HasKey(o => o.Id); });

        // Add Quiz -> QuizAttempt cascade delete relationship
        modelBuilder.Entity<QuizAttempt>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasOne<Quiz>().WithMany().HasForeignKey(a => a.QuizId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(a => a.Answers).WithOne(ans => ans.QuizAttempt!).HasForeignKey(ans => ans.QuizAttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAnswer>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasMany(a => a.SelectedOptions).WithOne(aso => aso.QuizAnswer!).HasForeignKey(aso => aso.QuizAnswerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAnswerOption>(b =>
        {
            b.HasKey(x => new { x.QuizAnswerId, x.OptionId });
        });
    }
}
