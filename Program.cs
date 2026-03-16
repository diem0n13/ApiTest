using System.Text;
using Course.Data;
using Course.Interfaces;
using Course.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<ApiDbContext>(x => x.UseSqlite("Data Source=Watch.db"));

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100; // Number of requests allowed per window
        limiterOptions.Window = TimeSpan.FromSeconds(1); // Time window duration
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst; // Process queued requests in order
        limiterOptions.QueueLimit = 10; // Maximum number of requests that can be queued
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddScoped<IAccount,AccountService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        var exist =  db.Database.CanConnect();
        if (!exist)
        { 
            db.Database.EnsureCreated();
        }
        else
        {
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await db.Database.MigrateAsync();
                }
        }
            
        
        
        db.Database.EnsureCreated();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("fixed");

app.Run();