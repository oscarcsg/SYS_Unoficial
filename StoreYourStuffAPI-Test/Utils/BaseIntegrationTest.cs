using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.Security;
using System.Net.Http.Headers;

namespace StoreYourStuffAPI_Test.Utils
{
    // Abstract para que no se pueda instanciar por sí sola
    public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        #region Attributes
        // Abstract para que las clases hijas puedan usarlas
        protected readonly HttpClient _client;
        protected readonly CustomWebApplicationFactory _factory;
        #endregion

        #region Constructors
        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }
        #endregion

        #region Methods
        // Método disponible para cualquier test que herede de esta clase
        protected async Task ResetDatabaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        // Método disponible para cualquier test que herede de esta clase
        protected async Task<int> AuthenticateClientAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var testUser = TestDataSeeder.GenerateUsers(1).First();
            db.Users.Add(testUser);
            await db.SaveChangesAsync();

            var token = tokenService.CreateToken(testUser);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return testUser.Id;
        }
        #endregion
    }
}
