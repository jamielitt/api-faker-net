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
    return sb.ToString();
});

app.Run();
