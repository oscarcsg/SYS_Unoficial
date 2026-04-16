using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.DTOs.Link;
using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.User;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI_Test.Utils;
using System.Net;
using System.Net.Http.Json;

namespace StoreYourStuffAPI_Test
{
    // Heredamos de BaseIntegrationTest para tener disponible el _client y la base de datos limpia de test
    public class UsersControllerTests : BaseIntegrationTest
    {
        // Al igual que en Categories, inyectamos el factory para levantar la app en memoria y conectar los cables
        public UsersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

        // GET Buscar usuario /api/users/search
        [Fact]
        public async Task SearchUser_ReturnsOkAndMatches_CuandoBuscamosPorAlias()
        {
            // Arrange
            // Arrancamos de cero para no ensuciarnos con lo que dejen los otros tests
            await ResetDatabaseAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Metemos unos usuarios de prueba a mano directo en EntityFramework
                db.Users.AddRange(
                    new User { Alias = "Goku_Super", Email = "goku@z.com", Password = "123" },
                    new User { Alias = "Vegeta_Prince", Email = "vegeta@z.com", Password = "123" },
                    new User { Alias = "Gohan_SSJ2", Email = "gohan@z.com", Password = "123" }
                );
                await db.SaveChangesAsync();
            }

            // Act
            // Le pegamos al endpoint de búsqueda pasando algo tentativo por Query Params
            var response = await _client.GetAsync("/api/users/search?search=Goku");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<List<UserPreviewDTO>>();

            // Validamos que vino algo y que justamente calza con Goku
            Assert.NotNull(result);
            Assert.Single(result); // Esperamos que sólo haya 1 resultado (Goku_Super)
            Assert.Equal("Goku_Super", result[0].Alias);
        }

        // GET Detalle /api/users/{userId}
        [Fact]
        public async Task GetUserById_ReturnsUser_CuandoExiste()
        {
            // Arrange
            await ResetDatabaseAsync();
            int targetUserId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var newUser = new User { Alias = "Piccolo", Email = "pic@z.com", Password = "123" };
                db.Users.Add(newUser);
                await db.SaveChangesAsync();
                targetUserId = newUser.Id;
            }

            // Act
            // Con el ID recién generado hacemos la petición para traer su ficha
            var response = await _client.GetAsync($"/api/users/{targetUserId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var userDetail = await response.Content.ReadFromJsonAsync<UserResponseDTO>();
            
            // Fuerte y al medio: tiene que ser Piccolo
            Assert.NotNull(userDetail);
            Assert.Equal("Piccolo", userDetail.Alias);
        }

        // POST Crear nuevo /api/users
        [Fact]
        public async Task CreateUser_ReturnsCreated_CuandoLosDatosSonValidos()
        {
            // Arrange
            await ResetDatabaseAsync();
            var registerDto = new UserCreateDTO
            {
                Alias = "Krilin",
                Email = "krilin@z.com",
                Password = "SuperPasswordSegura123!"
            };

            // Act
            // Hacemos que se registre un usuario nuevo por la API
            var response = await _client.PostAsJsonAsync("/api/users", registerDto);

            // Assert
            // Aquí esperamos un 201 Created porque se trata de una inserción limpia en base de datos.
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verificamos que los datos que vuelve el servidor coinciden
            var createdUser = await response.Content.ReadFromJsonAsync<UserResponseDTO>();
            Assert.NotNull(createdUser);
            Assert.Equal("Krilin", createdUser.Alias);
            Assert.Equal("krilin@z.com", createdUser.Email);
            Assert.True(createdUser.Id > 0, "EntityFramework debería haberle asignado un ID mayor a cero");
        }

        // PUT Actualizar perfil /api/users/profile
        [Fact]
        public async Task UpdateProfile_ReturnsOk_CuandoActualizamosNuestroPropioPerfil()
        {
            // Arrange
            await ResetDatabaseAsync();
            // Acá sí nos logueamos porque el update de perfil requiere [Authorize] para saber quién somos (mediante token decodificado)
            int loggedInUserId = await AuthenticateClientAsync(); 
            
            var updateDto = new UserUpdateDTO
            {
                Alias = "Nuevo_Alias_Poderoso",
                Email = "nuevo@poder.com"
            };

            // Act
            // Con el token ya metido en la cabecera (AuthenticateClientAsync lo hace por nosotros en test), tiramos el PUT
            var response = await _client.PutAsJsonAsync($"/api/users/profile", updateDto);

            // Assert
            // Revisamos bien el status (si todo piola, nos manda Ok con un mensajito)
            response.EnsureSuccessStatusCode();
            
            // Validamos que en la DB de test el cambio persistió y no fue solo humo
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedUser = db.Users.Find(loggedInUserId);
                
                Assert.NotNull(updatedUser);
                Assert.Equal("Nuevo_Alias_Poderoso", updatedUser.Alias);
                Assert.Equal("nuevo@poder.com", updatedUser.Email);
            }
        }

        // POST Iniciar sesión /api/users/login
        [Fact]
        public async Task Login_ReturnsOkAndToken_CuandoCredencialesPasanLaPrueba()
        {
            // Arrange
            await ResetDatabaseAsync();
            
            // Paso previo: Registrarlo bien pasando por la API para que la Password quede suculenta (hasheada) y no explote en la DB a pelo.
            var p = await _client.PostAsJsonAsync("/api/users", new UserCreateDTO 
            { 
                Alias = "Bulma", 
                Email = "bulma@capsule.corp", 
                Password = "PasswordLoca_123" 
            });
            p.EnsureSuccessStatusCode(); 

            // Ahora preparamos el DTO de login como lo enviaría Angular o React desde un frontal
            var loginDto = new LoginDTO
            {
                Email = "bulma@capsule.corp", // En este sistema se puede loguear por email 
                Password = "PasswordLoca_123"
            };

            // Act
            // Le clavamos la petición al endpoint
            var response = await _client.PostAsJsonAsync("/api/users/login", loginDto);

            // Assert
            // El login deberia darnos el valid OK (200) y soltar la llave maestra (el JWT)
            response.EnsureSuccessStatusCode();
            
            // El controlador hace un return Ok(new { message, token, userData }), por lo tanto
            // deserializamos a un Node de Json dinámico para verificar todo al vuelo:
            var jsonNode = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();
            Assert.NotNull(jsonNode);
            
            // Sacamos el token que mandó el server (es re largo, usamos ToString() para chequear null/empty)
            var tokenStr = jsonNode["token"]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(tokenStr), "Ups, el token no tendría que estar vacío");
            
            // También chequeamos que venga la data extraútil para el frontend
            Assert.Equal("bulma@capsule.corp", jsonNode["userData"]?["email"]?.ToString());
        }
        // GET Exploración general de usuarios /api/users
        [Fact]
        public async Task GetUsers_ReturnsOkYListaMinima_ParaElModoExploracion()
        {
            // Arrange
            await ResetDatabaseAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Alias = "RandomUser", Email = "random@x.com", Password = "123" });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/users");

            // Assert
            response.EnsureSuccessStatusCode();
            var users = await response.Content.ReadFromJsonAsync<List<UserResponseDTO>>();
            Assert.NotNull(users);
            Assert.Contains(users, u => u.Alias == "RandomUser");
        }

        // GET Búsqueda límite /api/users/search
        [Fact]
        public async Task SearchUser_ReturnsBadRequest_CuandoSearchEsMuyCortoORepetido()
        {
            // Arrange: no se necesita setup

            // Act
            var resCorto = await _client.GetAsync("/api/users/search?search=AB"); // 2 caracteres
            var resVacio = await _client.GetAsync("/api/users/search?search="); // Vacío

            // Assert
            // La validación dice que requiere mínimo 3 letras y no debe ser nulo, por lo que bloquea soltando BadRequests (400)
            Assert.Equal(HttpStatusCode.BadRequest, resCorto.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, resVacio.StatusCode);
        }

        // GET Detalle /api/users/{userId}
        [Fact]
        public async Task GetUserById_ReturnsNotFound_CuandoNoExisteElUsuario()
        {
            await ResetDatabaseAsync();
            var res = await _client.GetAsync("/api/users/999999"); // ID fantasma
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode); // La API debe escudarse bien y soltar 404
        }

        // GET Links Públicos de X usuario /api/users/{userId}/links
        [Fact]
        public async Task GetAllPublicUserLinks_ReturnsOk_YTraeSolamenteLosPublicos_SinDejarRastroPrivado()
        {
            // Arrange
            await ResetDatabaseAsync();
            int otherUserId = 66;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = otherUserId, Alias = "Otro", Email = "otro@test.com", Password = "123" });
                
                // Le colamos dos enlaces: uno privado (turbio) y uno hiper publico
                db.Links.AddRange(
                    new Link { OwnerId = otherUserId, Title = "El Publico", Url = "pub.com", IsPrivate = false },
                    new Link { OwnerId = otherUserId, Title = "El Privado", Url = "priv.com", IsPrivate = true }
                );
                await db.SaveChangesAsync();
            }

            // Act
            // Vamos como visitante anónimo o usuario logueado X a verle sus enlaces públicos
            var response = await _client.GetAsync($"/api/users/{otherUserId}/links");

            // Assert
            response.EnsureSuccessStatusCode();
            var links = await response.Content.ReadFromJsonAsync<List<LinkPreviewDTO>>();

            // Validación rigurosa de control de datos confidenciales
            Assert.NotNull(links);
            Assert.Single(links); // Solo deberia traer 1
            Assert.Equal("El Publico", links[0].Title); // Y debe ser justamente el que no es privado
        }

        // GET Categorias Públicas de X usuario /api/users/{userId}/categories
        [Fact]
        public async Task GetAllPublicUserCategories_ReturnsOk_SoloRevelaLasPublicas()
        {
            // Arrange
            await ResetDatabaseAsync();
            int targetUserId = 22;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = targetUserId, Alias = "Target", Email = "target@test.com", Password = "123" });
                
                // Categorias: publicas y privadas
                db.Categories.AddRange(
                    new Category { OwnerId = targetUserId, Name = "Cat publica", HexColor = "111", IsPrivate = false },
                    new Category { OwnerId = targetUserId, Name = "Cat privada", HexColor = "000", IsPrivate = true }
                );
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/users/{targetUserId}/categories");

            // Assert
            response.EnsureSuccessStatusCode();
            var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponseDTO>>();

            Assert.NotNull(categories);
            Assert.Single(categories);
            Assert.Equal("Cat publica", categories[0].Name);
        }

        // PUT Actualizar perfil - Conflictos /api/users/profile
        [Fact]
        public async Task UpdateProfile_ReturnsBadRequest_CuandoAliasOEmailYaExisten()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync(); // Mi ID
            int competitorId = 200;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Creamos un competidor que ya usa el alias y el correo "deseado"
                db.Users.Add(new User { Id = competitorId, Alias = "El_Jefe", Email = "jefe@test.com", Password = "123" });
                await db.SaveChangesAsync();
            }

            // Act
            // Primero tratamos de robarle el Alias
            var updateAliasDto = new UserUpdateDTO { Alias = "El_Jefe", Email = "original@test.com" };
            var responseAlias = await _client.PutAsJsonAsync("/api/users/profile", updateAliasDto);

            // Tratamos de robarle el Email
            var updateEmailDto = new UserUpdateDTO { Alias = "MyOriginalAlias", Email = "jefe@test.com" };
            var responseEmail = await _client.PutAsJsonAsync("/api/users/profile", updateEmailDto);

            // Assert
            // La base de datos y la API deben rebotarnos de esquina a esquina (400 BadRequest)
            Assert.Equal(HttpStatusCode.BadRequest, responseAlias.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, responseEmail.StatusCode);
        }

        // POST Login Failed /api/users/login
        [Fact]
        public async Task Login_ReturnsUnauthorized_CuandoCredencialesSonInvalidas()
        {
            // Arrange
            await ResetDatabaseAsync();
            await _client.PostAsJsonAsync("/api/users", new UserCreateDTO 
            { 
                Alias = "ElTriste", Email = "triste@test.com", Password = "ContraValida1" 
            });

            var loginDto = new LoginDTO { Email = "triste@test.com", Password = "Mal" };

            // Act
            var res = await _client.PostAsJsonAsync("/api/users/login", loginDto);

            // Assert
            // Clave mala => Puerta cerrada (401 Unauthorized)
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        // Tests extras de robustez
        [Fact]
        public async Task CreateUser_ReturnsBadRequest_CuandoFaltaDataRequerida()
        {
            await ResetDatabaseAsync();
            var registerDto = new UserCreateDTO
            {
                Email = "malo", // Email incompleto o no format
                // Falta Alias
                Password = "" // Password vacía
            };

            var response = await _client.PostAsJsonAsync("/api/users", registerDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsBadRequest_CuandoLosDatosSonNulosOInvalidos()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync(); 
            var updateDto = new UserUpdateDTO
            {
                Alias = "", // No permitido
                Email = "esto-no-es-un-mail" // Formato incorrecto
            };

            var response = await _client.PutAsJsonAsync($"/api/users/profile", updateDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
