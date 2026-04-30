using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// SQL Server
builder.Services.AddDbContext<FeedbackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<OpenAIService>();

// Pipeline
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

app.Run();