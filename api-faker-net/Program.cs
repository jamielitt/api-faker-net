using System.Net;
using System.Text;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Map("/{**route}", (HttpContext context) =>
{
    var httpVerb = context.Request.Method;
    var path = context.Request.Path;
    bool responseBody = false;
    int sequenceNumber = 0;
    int responseCode = 404;
    
    if (context.Request.Headers.TryGetValue("x-delay", out var delayValue))
    {
        var delay = delayValue.ToString();
    }

    if (context.Request.Headers.TryGetValue("x-set-response", out var response))
    {
        responseBody = true;
    }

    if (context.Request.Headers.TryGetValue("x-sequence", out var sequence))
    {
        if (!string.IsNullOrWhiteSpace(sequence))
        {
            sequenceNumber = int.Parse(sequence);
        }
    }

    if (context.Request.Headers.TryGetValue("x-set-httpresponsecode", out var result))
    {
        responseCode = int.Parse(result);
    }

    
    int status =
        Enum.IsDefined(typeof(HttpStatusCode), responseCode)
            ? responseCode
            : 404;

    return Results.Json(new { 
        httpVerb = httpVerb,
        path = path,
        response = !responseBody ? "x-set-response header not set. This will be used to load the cache for this endpoint with data" : "Response set the the body of the request",
        delay = (StringValues.IsNullOrEmpty(delayValue)) ? "No value set" : delayValue.ToString(),
        sequence = sequenceNumber == 0 ? "This will set the order of how the responses are returned from the cache with this endpoint" : sequenceNumber.ToString(),
        httpResponseCode = responseCode == 404 ? responseCode.ToString() : "x-set-httpresponsecode header not set",
    }, statusCode: status);
});

app.Run();