using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedbackMiner.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFeedbackModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SentimentLabel",
                table: "FeedbackItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "SentimentScore",
                table: "FeedbackItems",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SimilarityScore",
                table: "FeedbackItems",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ThemeClusterId",
                table: "FeedbackItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ThemeId",
                table: "FeedbackItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrgencyLevel",
                table: "FeedbackItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "UrgencyScore",
                table: "FeedbackItems",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessingRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeedbackItemCount = table.Column<int>(type: "int", nullable: false),
                    ClusterCount = table.Column<int>(type: "int", nullable: false),
                    ThemeCount = table.Column<int>(type: "int", nullable: false),
                    AverageClusterQuality = table.Column<double>(type: "float", nullable: false),
                    DuplicateDetectionPrecision = table.Column<double>(type: "float", nullable: false),
                    AverageThemeRelevance = table.Column<double>(type: "float", nullable: false),
                    AverageActionUsefulness = table.Column<double>(type: "float", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClusteringAlgorithm = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledDigestRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeedbackCount = table.Column<int>(type: "int", nullable: false),
                    NewThemesCount = table.Column<int>(type: "int", nullable: false),
                    CriticalActionItemsCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DigestContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientEmailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledDigestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelevanceScore = table.Column<double>(type: "float", nullable: false),
                    FeedbackCount = table.Column<int>(type: "int", nullable: false),
                    AverageSentimentScore = table.Column<double>(type: "float", nullable: false),
                    AverageUrgencyScore = table.Column<double>(type: "float", nullable: false),
                    ImpactScore = table.Column<double>(type: "float", nullable: false),
                    AffectedProductAreasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedSegmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThemeHashCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AverageThemeRelevanceScore = table.Column<double>(type: "float", nullable: false),
                    ThemeRelevanceMetPercentage = table.Column<double>(type: "float", nullable: false),
                    ClusteringPrecision = table.Column<double>(type: "float", nullable: false),
                    DuplicateDetectionRate = table.Column<double>(type: "float", nullable: false),
                    AverageRecommendationUsefulnessScore = table.Column<double>(type: "float", nullable: false),
                    RecommendationUsefulnessMetPercentage = table.Column<double>(type: "float", nullable: false),
                    AverageSilhouetteScore = table.Column<double>(type: "float", nullable: false),
                    OverallQualityScore = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_ProcessingRuns_ProcessingRunId",
                        column: x => x.ProcessingRunId,
                        principalTable: "ProcessingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedEffort = table.Column<int>(type: "int", nullable: false),
                    ImpactScore = table.Column<double>(type: "float", nullable: false),
                    UsefulnessRating = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedTeam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedAreasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BenefitSegmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionRecommendations_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThemeClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClusterNumber = table.Column<int>(type: "int", nullable: false),
                    ProcessingRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroidEmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    AverageSimilarity = table.Column<double>(type: "float", nullable: false),
                    SilhouetteScore = table.Column<double>(type: "float", nullable: false),
                    SuggestedThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeClusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemeClusters_ProcessingRuns_ProcessingRunId",
                        column: x => x.ProcessingRunId,
                        principalTable: "ProcessingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThemeClusters_Themes_SuggestedThemeId",
                        column: x => x.SuggestedThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ThemeEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelevanceScore = table.Column<double>(type: "float", nullable: false),
                    MetRelevanceThreshold = table.Column<bool>(type: "bit", nullable: false),
                    EstimatedAffectedCustomers = table.Column<int>(type: "int", nullable: false),
                    FeedbackPercentage = table.Column<double>(type: "float", nullable: false),
                    Trend = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemeEvaluations_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThemeEvaluations_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionRecommendationEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsefulnessScore = table.Column<double>(type: "float", nullable: false),
                    MetUsefulnessThreshold = table.Column<bool>(type: "bit", nullable: false),
                    FeasibilityScore = table.Column<double>(type: "float", nullable: false),
                    ExpectedImpact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedTimelineDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualEffortSpent = table.Column<int>(type: "int", nullable: true),
                    ImplementationFeedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImplementedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRecommendationEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionRecommendationEvaluations_ActionRecommendations_ActionRecommendationId",
                        column: x => x.ActionRecommendationId,
                        principalTable: "ActionRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionRecommendationEvaluations_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackItems_ThemeClusterId",
                table: "FeedbackItems",
                column: "ThemeClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackItems_ThemeId",
                table: "FeedbackItems",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecommendationEvaluations_ActionRecommendationId",
                table: "ActionRecommendationEvaluations",
                column: "ActionRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecommendationEvaluations_EvaluationRunId",
                table: "ActionRecommendationEvaluations",
                column: "EvaluationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecommendations_ThemeId",
                table: "ActionRecommendations",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_ProcessingRunId",
                table: "EvaluationRuns",
                column: "ProcessingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledDigestRuns_WeekStart",
                table: "ScheduledDigestRuns",
                column: "WeekStart");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeClusters_ProcessingRunId",
                table: "ThemeClusters",
                column: "ProcessingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeClusters_SuggestedThemeId",
                table: "ThemeClusters",
                column: "SuggestedThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeEvaluations_EvaluationRunId",
                table: "ThemeEvaluations",
                column: "EvaluationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeEvaluations_ThemeId",
                table: "ThemeEvaluations",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Themes_Label",
                table: "Themes",
                column: "Label");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackItems_ThemeClusters_ThemeClusterId",
                table: "FeedbackItems",
                column: "ThemeClusterId",
                principalTable: "ThemeClusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackItems_Themes_ThemeId",
                table: "FeedbackItems",
                column: "ThemeId",
                principalTable: "Themes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackItems_ThemeClusters_ThemeClusterId",
                table: "FeedbackItems");

            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackItems_Themes_ThemeId",
                table: "FeedbackItems");

            migrationBuilder.DropTable(
                name: "ActionRecommendationEvaluations");

            migrationBuilder.DropTable(
                name: "ScheduledDigestRuns");

            migrationBuilder.DropTable(
                name: "ThemeClusters");

            migrationBuilder.DropTable(
                name: "ThemeEvaluations");

            migrationBuilder.DropTable(
                name: "ActionRecommendations");

            migrationBuilder.DropTable(
                name: "EvaluationRuns");

            migrationBuilder.DropTable(
                name: "Themes");

            migrationBuilder.DropTable(
                name: "ProcessingRuns");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackItems_ThemeClusterId",
                table: "FeedbackItems");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackItems_ThemeId",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "SentimentLabel",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "SentimentScore",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "SimilarityScore",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "ThemeClusterId",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "ThemeId",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "UrgencyLevel",
                table: "FeedbackItems");

            migrationBuilder.DropColumn(
                name: "UrgencyScore",
                table: "FeedbackItems");
        }
    }
}
