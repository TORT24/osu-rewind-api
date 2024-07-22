using ORewindApi.Handlers;
using Middleware.SwaggerAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RedditHandler>();
builder.Configuration.AddUserSecrets<RedditHandler>();
builder.Services.AddSingleton<OsuApiHandler>();
builder.Configuration.AddUserSecrets<OsuApiHandler>();
builder.Services.AddHttpClient<OsuApiHandler>();


var app = builder.Build();

var redditHandler = app.Services.GetRequiredService<RedditHandler>();
redditHandler.StatusCheck();
// var osuHandler = app.Services.GetRequiredService<OsuApiHandler>();
// var test = await osuHandler.GetUserInfo("fsfsdadsfafdsgdfsdsfdsffsdsdfdsf");
// Console.WriteLine(test.Username);

app.UseAuthorization();
app.UseSwaggerAuth();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
