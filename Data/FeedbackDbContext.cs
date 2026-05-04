using Microsoft.EntityFrameworkCore;

public class FeedbackDbContext : DbContext
{
    public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options)
        : base(options) { }

    public DbSet<FeedbackItem> FeedbackItems { get; set; }
    public DbSet<Theme> Themes { get; set; }
    public DbSet<ThemeCluster> ThemeClusters { get; set; }
    public DbSet<ActionRecommendation> ActionRecommendations { get; set; }
    public DbSet<ProcessingRun> ProcessingRuns { get; set; }

    // Evaluation entities
    public DbSet<EvaluationRun> EvaluationRuns { get; set; }
    public DbSet<ThemeEvaluation> ThemeEvaluations { get; set; }
    public DbSet<ActionRecommendationEvaluation> ActionRecommendationEvaluations { get; set; }
    public DbSet<ScheduledDigestRun> ScheduledDigestRuns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FeedbackItem relationships
        modelBuilder.Entity<FeedbackItem>()
            .HasOne(f => f.Theme)
            .WithMany(t => t.FeedbackItems)
            .HasForeignKey(f => f.ThemeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FeedbackItem>()
            .HasOne(f => f.ThemeCluster)
            .WithMany(tc => tc.FeedbackItems)
            .HasForeignKey(f => f.ThemeClusterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Theme relationships
        modelBuilder.Entity<Theme>()
            .HasMany(t => t.Clusters)
            .WithOne(tc => tc.SuggestedTheme)
            .HasForeignKey(tc => tc.SuggestedThemeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Theme>()
            .HasMany(t => t.ActionRecommendations)
            .WithOne(ar => ar.Theme)
            .HasForeignKey(ar => ar.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ThemeCluster relationships
        modelBuilder.Entity<ThemeCluster>()
            .HasMany(tc => tc.FeedbackItems)
            .WithOne(f => f.ThemeCluster)
            .HasForeignKey(f => f.ThemeClusterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index for common queries
        modelBuilder.Entity<FeedbackItem>()
            .HasIndex(f => f.ThemeClusterId);

        modelBuilder.Entity<FeedbackItem>()
            .HasIndex(f => f.ThemeId);

        modelBuilder.Entity<Theme>()
            .HasIndex(t => t.Label);

        modelBuilder.Entity<ActionRecommendation>()
            .HasIndex(ar => ar.ThemeId);

        modelBuilder.Entity<ProcessingRun>()
            .HasMany(pr => pr.Clusters)
            .WithOne()
            .HasForeignKey(tc => tc.ProcessingRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure EvaluationRun relationships
        modelBuilder.Entity<EvaluationRun>()
            .HasOne(er => er.ProcessingRun)
            .WithMany()
            .HasForeignKey(er => er.ProcessingRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EvaluationRun>()
            .HasMany(er => er.ThemeEvaluations)
            .WithOne(te => te.EvaluationRun)
            .HasForeignKey(te => te.EvaluationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EvaluationRun>()
            .HasMany(er => er.ActionRecommendationEvaluations)
            .WithOne(are => are.EvaluationRun)
            .HasForeignKey(are => are.EvaluationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ThemeEvaluation relationships
        modelBuilder.Entity<ThemeEvaluation>()
            .HasOne(te => te.Theme)
            .WithMany()
            .HasForeignKey(te => te.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ActionRecommendationEvaluation relationships
        modelBuilder.Entity<ActionRecommendationEvaluation>()
            .HasOne(are => are.ActionRecommendation)
            .WithMany()
            .HasForeignKey(are => are.ActionRecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for evaluation queries
        modelBuilder.Entity<EvaluationRun>()
            .HasIndex(er => er.ProcessingRunId);

        modelBuilder.Entity<ThemeEvaluation>()
            .HasIndex(te => te.ThemeId);

        modelBuilder.Entity<ActionRecommendationEvaluation>()
            .HasIndex(are => are.ActionRecommendationId);

        modelBuilder.Entity<ScheduledDigestRun>()
            .HasIndex(sdr => sdr.WeekStart);
    }
}
