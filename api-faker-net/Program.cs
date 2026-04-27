using System.Text;

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
    StringBuilder sb = new StringBuilder();
    sb.AppendLine($"HTTP Verb being used: {httpVerb}");
    sb.AppendLine($"Path used: {path}");
    
    if (context.Request.Headers.TryGetValue("x-delay", out var delayValue))
    {
        var delay = delayValue.ToString(); // headers can have multiple values
        sb.AppendLine($"x-delay header present with value: {delay}");
    }
    else
    {
        sb.AppendLine("x-delay header not present");
    }

    if (context.Request.Headers.TryGetValue("x-set-response", out var response))
    {
        var delay = delayValue.ToString(); // headers can have multiple values
        sb.AppendLine($"x-set-response header. This would set the response of this request in the cache");
    }
    else
    {
        sb.AppendLine("x-set-response header not set. This will be used to load the cache for this endpoint with data");
    }

    if (context.Request.Headers.TryGetValue("x-set-response", out var sequence))
    {
        var delay = delayValue.ToString(); // headers can have multiple values
        sb.AppendLine($"x-sequence header. This will set the order of how the responses are returned from the cache with this endpoint");
    }
    else
    {
        sb.AppendLine("x-sequence header not set. This is only for single endpoints, or the first");
    }

    if (context.Request.Headers.TryGetValue("x-set-httpresponsecode", out var responsecode))
    {
        var delay = delayValue.ToString(); // headers can have multiple values
        sb.AppendLine($"x-set-httpresponsecode header. This will set the HTTP Rsponse Code to {responsecode}");
    }
    else
    {
        sb.AppendLine("x-set-httpresponsecode header not set.");
    }
    return sb.ToString();
});

app.Run();