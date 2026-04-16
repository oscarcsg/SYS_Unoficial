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
        // Obtener todas las categorias (GET /api/categories)
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

        // GET Detalle (GET /api/categories/{categoryId})
        [Fact]
        public async Task GetCategoryDetail_ReturnsOkAndCategory_CuandoEsDelUsuario()
        {
            // Arrange
            // Siempre arrancamos de cero con la base de datos limpia para que cada test sea independiente y no nos traiga de cabeza
            await ResetDatabaseAsync();
            
            // Hacemos login simulado y nos guardamos el ID del usuario como referencia
            int loggedInUserId = await AuthenticateClientAsync();
            int targetCategoryId;

            // Metemos mano directo en la BDD para colar una categoría de prueba lista para consumir
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var myCategory = new Category { Name = "Categoría detalle", HexColor = "FF0000", OwnerId = loggedInUserId };
                db.Categories.Add(myCategory);
                await db.SaveChangesAsync();
                
                targetCategoryId = myCategory.Id;
            }

            // Act
            // Le pegamos al endpoint armando la ruta con el id recién generado
            var response = await _client.GetAsync($"/api/categories/{targetCategoryId}");

            // Assert
            // Nos aseguramos de que el código HTTP sea nivel 2xx (normalmente 200 OK)
            response.EnsureSuccessStatusCode(); 
            var returnedCategory = await response.Content.ReadFromJsonAsync<CategoryResponseDTO>();

            // Validamos que los datos que trae tengan sentido y no haya perdido cosas por el camino
            Assert.NotNull(returnedCategory);
            Assert.Equal("Categoría detalle", returnedCategory.Name);
            Assert.Equal(loggedInUserId, returnedCategory.OwnerId);
        }

        [Fact]
        public async Task GetCategoryDetail_ReturnsNotFound_CuandoIntentamosCuriosearLoDeOtro()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync(); // Al loguearnos nuestro user será asignado, el ID no importa acá
            int categoryIdFromOtherUser;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Creamos una categoría que le pertenezca a otro loco (User 999) para forzar el fallo de seguridad
                var otherUserCategory = new Category { Name = "Secreto", HexColor = "000000", OwnerId = 999 };
                db.Categories.Add(otherUserCategory);
                await db.SaveChangesAsync();
                categoryIdFromOtherUser = otherUserCategory.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/categories/{categoryIdFromOtherUser}");

            // Assert
            // El controlador busca por ID y por current UserId. Como no cuadran, nos tiene que escupir un 404 (NotFound).
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        // POST Crear (POST /api/categories)
        [Fact]
        public async Task CreateCategory_ReturnsCreated_CuandoMandamosDatosValidos()
        {
            // Arrange
            await ResetDatabaseAsync();
            int loggedInUserId = await AuthenticateClientAsync();

            // Armamos el DTO de entrada. Esto simula tal cual el JSON que enviaría el frontend en el body temporal
            var newCategoryDto = new CategoryCreateDTO
            {
                Name = "Nueva Categoría",
                HexColor = "AABBCC",
                IsPrivate = true
            };

            // Act
            // Disparamos el POST y dejamos que la magia del .NET serialice a json automáticamente
            var response = await _client.PostAsJsonAsync("/api/categories", newCategoryDto);

            // Assert
            // Debe responder un 201 Created (que corresponde y marca las buenas prácticas de APIs REST)
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            
            var createdCategory = await response.Content.ReadFromJsonAsync<CategoryResponseDTO>();
            Assert.NotNull(createdCategory);
            
            // Verificamos que realmente se haya insertado algo con sentido. El owner lo tuvo que haber inyectado del token.
            Assert.Equal("Nueva Categoría", createdCategory.Name);
            Assert.Equal(loggedInUserId, createdCategory.OwnerId);
            Assert.True(createdCategory.Id > 0, "No se generó el Id, fíjate en EntityFramework");

            // Comprobamos de yapa el Header 'Location' que se arma con "CreatedAtAction" en el controllador
            Assert.NotNull(response.Headers.Location);
        }

        // PUT Actualizar (PUT /api/categories/{categoryId})
        [Fact]
        public async Task UpdateCategory_ReturnsNoContent_CuandoModificamosUnaPropia()
        {
            // Arrange
            await ResetDatabaseAsync();
            int loggedInUserId = await AuthenticateClientAsync();
            int categoryId;

            // Inyectamos estado previo
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cat = new Category { Name = "Viejo Nombre", HexColor = "111111", OwnerId = loggedInUserId };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
                categoryId = cat.Id;
            }

            // Preparamos lo que vamos a pisar. Ahora es distinto:
            var updateDto = new CategoryUpdateDTO
            {
                Name = "Nombre Actualizado",
                HexColor = "999999",
                IsPrivate = false
            };

            // Act
            // Le pegamos con un PUT apuntando específico a la URL con el /{id}
            var response = await _client.PutAsJsonAsync($"/api/categories/{categoryId}", updateDto);

            // Assert
            // El estándar en updates que no tienen por qué devolverte todo el objeto json es un limpio 204 NoContent.
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Para quedarnos tranquilos y que no sea humo, vamos físicamente a la DB a ver si mutó de verdad
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedCatInDb = db.Categories.Find(categoryId);
                Assert.NotNull(updatedCatInDb);
                Assert.Equal("Nombre Actualizado", updatedCatInDb.Name);
                Assert.Equal("999999", updatedCatInDb.HexColor);
            }
        }

        [Fact]
        public async Task UpdateCategory_ReturnsForbid_CuandoIntentamosPisarUnaDeOtroUser()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            int categoryIdFromOtherUser;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cat = new Category { Name = "Intocable", HexColor = "000000", OwnerId = 999 };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
                categoryIdFromOtherUser = cat.Id;
            }

            var updateDto = new CategoryUpdateDTO { Name = "Me la quedo", HexColor = "FFFFFF", IsPrivate = false };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/categories/{categoryIdFromOtherUser}", updateDto);

            // Assert
            // Al comparar Owner Ids va a pinchar adrede y botón; el controlador tiene que levantar escudo 403 Forbid si estamos robando.
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        // DELETE (DELETE /api/categories/{categoryId})
        [Fact]
        public async Task DeleteCategory_RemovesFromDbAndReturnsNoContent_CuandoEsPropia()
        {
            // Arrange
            await ResetDatabaseAsync();
            int loggedInUserId = await AuthenticateClientAsync();
            int categoryIdToRemove;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cat = new Category { Name = "A Borrar", HexColor = "111111", OwnerId = loggedInUserId };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
                categoryIdToRemove = cat.Id;
            }

            // Act
            var response = await _client.DeleteAsync($"/api/categories/{categoryIdToRemove}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Doble chequeo en frío. Validamos que palmó de verdad la fila y ya no existe
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deletedCat = db.Categories.Find(categoryIdToRemove);
                
                // Si esto es Null, logramos matar el entity con el controlador de manera exitosa
                Assert.Null(deletedCat);
            }
        }

        [Fact]
        public async Task DeleteCategory_ReturnsForbid_CuandoIntentamosEliminarLoDeOtro()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            int otherUserCategoryId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cat = new Category { Name = "Otra categoria", HexColor = "222222", OwnerId = 999 };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
                otherUserCategoryId = cat.Id;
            }

            // Act
            var response = await _client.DeleteAsync($"/api/categories/{otherUserCategoryId}");

            // Assert
            // Nos tiene que dar portazo en la cara (403). Borrar entidades de otros es fallo crítico.
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Test extra de contundencia (Edge cases)
        [Fact]
        public async Task GetCategoryDetail_ReturnsNotFound_CuandoLaCategoriaNoExiste()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var response = await _client.GetAsync($"/api/categories/999999");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_CuandoFaltanDatosRequeridos()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var newCategoryDto = new CategoryCreateDTO { HexColor = "AABBCC", IsPrivate = true }; // Falta Name intencionalmente
            var response = await _client.PostAsJsonAsync("/api/categories", newCategoryDto);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNotFound_CuandoLaCategoriaNoExiste()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var updateDto = new CategoryUpdateDTO { Name = "Ghost", HexColor = "000000", IsPrivate = false };
            var response = await _client.PutAsJsonAsync($"/api/categories/999999", updateDto);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNotFound_CuandoLaCategoriaNoExiste()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var response = await _client.DeleteAsync($"/api/categories/999999");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
        #endregion

        #region Methods
        #endregion
    }
}
