using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SK_AccountingMoney.Data;
using SK_AccountingMoney.Services;
using System.Text;

namespace SK_AccountingMoney
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var configuration = builder.Configuration;
            var jwtSecret = configuration["Jwt:Secret"] ?? "WVGW/bu2NzJzovswCNJeq/ZYX4/ZOTK9q8hQ03mpEgA=";
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "SK_AccountingMoney";
            var jwtAudience = configuration["Jwt:Audience"] ?? "SK_AccountingMoney";
            var botToken = configuration["Telegram:BotToken"] ?? "8506709542:AAGMe1-mruA5EIKvExa26PWPilVoVsl-kFo";

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // JSON serialization settings
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddScoped<IBalanceService, BalanceService>();

            builder.Services.AddSingleton(new TelegramAuthService(botToken));
            builder.Services.AddSingleton(new JwtService(jwtSecret, jwtIssuer, jwtAudience));            

            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated(); 
                
                if (!dbContext.Users.Any())
                {
                    dbContext.Users.AddRange(
                        new Models.User
                        {
                            TelegramId = 527514644,
                            UserName = "olga_antonikova",                                                        
                        },
                        new Models.User
                        {
                            TelegramId = 491581922,
                            UserName = "krasnosergey",                                                       
                        }
                    );
                    dbContext.SaveChanges();
                }

                if (!dbContext.SharedBalances.Any())
                {                    
                    dbContext.SharedBalances.Add(new Models.SharedBalance
                    {
                        Name = "Main",
                        Balance = 0.00m, 
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    dbContext.SaveChanges();                    
                }
            }

            app.UseStaticFiles();            
            app.UseCors("AllowAll");            
            app.UseRouting();            
            app.MapControllers();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapFallback(async context =>
            {                
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("API endpoint not found");
                    return;
                }
                
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync("wwwroot/index.html");
            });

            app.Run();
        }
    }
}
