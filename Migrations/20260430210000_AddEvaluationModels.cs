using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedbackMiner.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ProcessingRunId to Theme table
            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingRunId",
                table: "Themes",
                type: "uniqueidentifier",
                nullable: true);

            // Create EvaluationRuns table
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
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
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

            // Create ThemeEvaluations table
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
                    Trend = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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

            // Create ActionRecommendationEvaluations table
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
                    ExpectedImpact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedTimelineDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualEffortSpent = table.Column<int>(type: "int", nullable: true),
                    ImplementationFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
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

            // Create ScheduledDigestRuns table
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
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DigestContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientEmailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledDigestRuns", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_ProcessingRunId",
                table: "EvaluationRuns",
                column: "ProcessingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeEvaluations_ThemeId",
                table: "ThemeEvaluations",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecommendationEvaluations_ActionRecommendationId",
                table: "ActionRecommendationEvaluations",
                column: "ActionRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledDigestRuns_WeekStart",
                table: "ScheduledDigestRuns",
                column: "WeekStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables
            migrationBuilder.DropTable("ActionRecommendationEvaluations");
            migrationBuilder.DropTable("ThemeEvaluations");
            migrationBuilder.DropTable("ScheduledDigestRuns");
            migrationBuilder.DropTable("EvaluationRuns");

            // Remove ProcessingRunId column from Theme table
            migrationBuilder.DropColumn(
                name: "ProcessingRunId",
                table: "Themes");
        }
    }
}
