using Microsoft.EntityFrameworkCore;
using CarPooling.Data;
using CarPooling.Services;
using System;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=carpooling;Username=postgres;Password=postgres";

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


//Services
//builder.Services.AddScoped<VoznjaService>();
builder.Services.AddScoped<PresedanjeService1>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
     policy.WithOrigins("https://pronadjivoznju.lovable.app", "http://localhost:8081")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.MapControllers();

app.Run();