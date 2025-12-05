using FastReport.Utils;

var builder = WebApplication.CreateBuilder(args);

// FIX dla Render / Docker: wyłącz private font collection (bo nie działa na Linux)
Config.UseLegacyPreview = false;
Config.EnablePrivateFontCollection = false;
// Add services to the container.
builder.Services.AddControllers();

// CORS – dopuszczamy każde żądanie (tylko do testów!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Map controllers
app.MapControllers();

// Test endpoint
app.MapGet("/", () => "FastReportService działa! Endpoint PDF: /reports/test");

app.Run();
