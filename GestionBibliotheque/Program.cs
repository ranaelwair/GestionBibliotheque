using System.Globalization;
using GestionBibliotheque.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("BibliothequeContext")
                ?? throw new InvalidOperationException("Connection string 'BibliothequeContext' not found.");

            builder.Services.AddDbContext<BibliothequeContext>(options => options.UseSqlServer(connectionString));

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Données de test (et migrations) au démarrage
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BibliothequeContext>();
                DbInitializer.Initialize(context);
            }

            // Affichage des dates et montants en français (Canada) : 24/09/2026, 2,50 $
            var fr = new CultureInfo("fr-CA");
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(fr),
                SupportedCultures = new[] { fr },
                SupportedUICultures = new[] { fr }
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
