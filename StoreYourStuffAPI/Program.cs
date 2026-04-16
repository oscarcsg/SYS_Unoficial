using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.Security;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// CORS para angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // Angular suele correr en el puerto 4200 por defecto
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Tell the API to use AppDbContext and to connect to SQLServer
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllers();

// This makes the url lowercase
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Service for hashing
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

// Service for token
builder.Services.AddScoped<ITokenService, TokenService>();

// Service for token authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Review the math sign with the secret key
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),

            // Review the creator name is mine
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],

            // Review it go forward the app
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],

            // Review caducity
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No mercy time with rotten tokens
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Activar el CORS
app.UseCors("AllowAngular");

// Token autentications
app.UseAuthentication(); // Make sure the token is real
app.UseAuthorization();  // Allowed to enter authorized sections?

app.MapControllers();

app.Run();