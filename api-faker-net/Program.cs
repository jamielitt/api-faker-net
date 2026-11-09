using api_faker_net.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiFaker(builder.Configuration);

// TODO: Add to OpenApi spec
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapApiFaker();

app.Run();
