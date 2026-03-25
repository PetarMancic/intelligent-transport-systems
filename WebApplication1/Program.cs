using Microsoft.EntityFrameworkCore;
using CarPooling.Data;
using CarPooling.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// ── Baza podataka ─────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=carpooling;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


// ── Servisi ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<VoznjaService>();
builder.Services.AddControllers();

// ── CORS — dozvoljava Lovable frontend da gada API ────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
     policy.WithOrigins("https://pronadjivoznju.lovable.app", "http://localhost:8081")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ── Swagger (opciono, korisno za testiranje) ──────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.MapControllers();

app.Run();