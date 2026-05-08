using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:4300",
                "https://localhost:4300")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

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
builder.Services.AddScoped<SummarizeService>();
builder.Services.AddScoped<DataSourceManager>();

// Background Services
builder.Services.AddHostedService<WeeklyDigestBackgroundService>();

// Data Services
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

// Text Processing Pipeline
builder.Services.AddScoped<ITextProcessor, TextCleaner>();
builder.Services.AddScoped<ITextProcessor, LanguageDetector>();
builder.Services.AddScoped<ITextProcessor, PiiRedactor>();
builder.Services.AddScoped<TextProcessingPipeline>();

var app = builder.Build();

app.UseCors("Frontend");

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Database migration failed during startup. The API will continue running, but database-backed endpoints may fail until the connection string or SQL Server authentication is fixed. Error: {ex.Message}");
    }
}

app.Run();
