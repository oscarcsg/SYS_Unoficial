using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI.Security;
using StoreYourStuffAPI_Test.Utils;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoreYourStuffAPI_Test
{
    // IClassFixture hace una sola api para toda esta clase de test
    public class CategoriesControllerTests : BaseIntegrationTest    
    {
        #region Attributes
        #endregion

        #region Constructors
        // Este constructor ejecuta antes de los test (en el [Fact])
        public CategoriesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }
        #endregion

        #region Tests
        // Obtener todas las categorias
        [Fact]
        public async Task GetCategories_ReturnsOkAndList_WhenCategoriesExist()
        {
            // Arrange
            await ResetDatabaseAsync();
            int loggedInUserId = await AuthenticateClientAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Añadir las 4 categorías del sistema
                var fakeSystemCategories = TestDataSeeder.GenerateSystemCategories();
                db.Categories.AddRange(fakeSystemCategories);

                // Añadir 3 categorías creadas específicamente por este usuario
                var userCategories = new List<Category>
                {
                    new Category { Name = "Mi Categoría 1", HexColor = "111111", OwnerId = loggedInUserId },
                    new Category { Name = "Mi Categoría 2", HexColor = "222222", OwnerId = loggedInUserId },
                    new Category { Name = "Mi Categoría 3", HexColor = "333333", OwnerId = loggedInUserId }
                };
                db.Categories.AddRange(userCategories);

                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/categories");

            // Assert
            response.EnsureSuccessStatusCode();
            var returnedCategories = await response.Content.ReadFromJsonAsync<List<CategoryResponseDTO>>();

            Assert.NotNull(returnedCategories);

            // Este endpoint solo debe traer las categories del usuario, NO las del sistema
            Assert.Equal(3, returnedCategories.Count);

            // Comprobar que la lógica de mapeo funciona verificando un nombre
            Assert.Contains(returnedCategories, c => c.Name == "Mi Categoría 1");
        }
        #endregion

        #region Methods
        #endregion
    }
}
