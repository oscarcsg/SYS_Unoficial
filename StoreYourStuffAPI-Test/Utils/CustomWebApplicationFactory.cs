using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using System.Linq;

namespace StoreYourStuffAPI_Test.Utils
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // 1. BARRIDO TOTAL: Buscamos TODOS los servicios cuya interfaz contenga "DbContextOptions"
                // Esto atrapa DbContextOptions, DbContextOptions<AppDbContext>, y cualquier otra variante oculta.
                var optionsDescriptors = services
                    .Where(d => d.ServiceType.Name.Contains("DbContextOptions"))
                    .ToList();

                // 2. Los eliminamos uno por uno
                foreach (var descriptor in optionsDescriptors)
                {
                    services.Remove(descriptor);
                }

                // 3. (Opcional pero recomendado) Hacemos lo mismo con las conexiones físicas de base de datos
                var connectionDescriptors = services
                    .Where(d => d.ServiceType.Name.Contains("DbConnection"))
                    .ToList();

                foreach (var descriptor in connectionDescriptors)
                {
                    services.Remove(descriptor);
                }

                // 4. Con el terreno 100% esterilizado, plantamos In-Memory
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("BaseDeDatosParaTests");
                });
            });
        }
    }
}