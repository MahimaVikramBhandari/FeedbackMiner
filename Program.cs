using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// SQL Server
builder.Services.AddDbContext<FeedbackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AI Services
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<SentimentAnalysisService>();
builder.Services.AddScoped<ThemeLabelingService>();
builder.Services.AddScoped<ActionRecommendationService>();

// Business Services
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<ClusteringService>();
builder.Services.AddScoped<FeedbackProcessingService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<EvaluationMetricsService>();
builder.Services.AddScoped<ScheduledDigestService>();
builder.Services.AddScoped<EvaluationNotebookService>();
builder.Services.AddScoped<DataSourceManager>();

// Background Services
builder.Services.AddHostedService<WeeklyDigestBackgroundService>();

// Data Services
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

// Text Processing Pipeline
builder.Services.AddScoped<TextProcessingPipeline>(sp =>
{
    return new TextProcessingPipeline(new List<ITextProcessor>
    {
        new TextCleaner(),
        new PiiRedactor(),
        new LanguageDetector(sp.GetRequiredService<OpenAIService>())
    });
});

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
    db.Database.Migrate();
}

app.Run();
