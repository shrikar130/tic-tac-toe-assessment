using TicTacToe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSingleton<GameService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy => policy
        .WithOrigins(
            "http://localhost:4200",
            "https://tic-tac-toe-assessment-fr1.onrender.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("Angular");

app.MapControllers();

app.Run();

public partial class Program { }