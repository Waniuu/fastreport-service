var builder = WebApplication.CreateBuilder(args);

// 🔥 FIX for Linux + Docker + FastReport
Environment.SetEnvironmentVariable("FASTREPORT_NOCUSTOMFONTS", "1");
Environment.SetEnvironmentVariable("FASTREPORT_NOGLOBALFONT", "1");

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/", () => "FastReport działa na Render!");

app.Run();
