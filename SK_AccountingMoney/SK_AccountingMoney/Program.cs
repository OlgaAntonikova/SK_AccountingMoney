using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Data;

namespace SK_AccountingMoney
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Настройки сериализации JSON
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

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
                            Balance = 0.00m,                            
                        },
                        new Models.User
                        {
                            TelegramId = 491581922,
                            UserName = "krasnosergey",
                            Balance = 0.00m,                            
                        }
                    );
                    dbContext.SaveChanges();
                }
            }

            app.UseStaticFiles();            
            app.UseCors("AllowAll");            
            app.UseRouting();            
            app.MapControllers(); 
            
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
