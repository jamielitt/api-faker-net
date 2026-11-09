using api_faker_net.Configuration;
using api_faker_net.Handlers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiFaker(builder.Configuration);

// TODO: Add to OpenApi spec
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Map("/{**route}", (HttpContext context, IRequestHandler handler) => handler.HandleAsync(context));

app.Run();
