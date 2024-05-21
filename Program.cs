using ORewindApi;
using Middleware.SwaggerAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RedditHandler>();
builder.Configuration.AddUserSecrets<RedditHandler>();

var app = builder.Build();

var redditHandler = app.Services.GetRequiredService<RedditHandler>();
redditHandler.StatusCheck();

app.UseAuthorization();
app.UseSwaggerAuth();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
