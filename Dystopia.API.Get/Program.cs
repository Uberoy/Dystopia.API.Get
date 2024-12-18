var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5952");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services
    .AddCors(options =>
        options.AddPolicy(
            "Support",
            policy =>
                policy
                    .WithOrigins("http://localhost:7777", "http://localhost:7151", "http://localhost:7173") // Explicit origins
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
        )
    );

var app = builder.Build();

// Ensure CORS middleware runs early in the pipeline
app.UseCors("Support");

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/tickets", async (HttpContext context) =>
{
    var start = context.Request.Query["start"];
    var count = context.Request.Query["count"];

    if (!int.TryParse(start, out var startValue) || !int.TryParse(count, out var countValue))
    {
        return Results.BadRequest("Invalid 'start' or 'count' query parameter.");
    }

    var repoUri = Environment.GetEnvironmentVariable("REPO_URI") ?? "http://repo:6050";
    using var httpClient = new HttpClient
    {
        BaseAddress = new Uri(repoUri)
    };

    var response = await httpClient.GetAsync($"/tickets?start={start}&count={count}");
    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    var data = await response.Content.ReadAsStringAsync();
    return Results.Content(data, "application/json");
});

app.Run();