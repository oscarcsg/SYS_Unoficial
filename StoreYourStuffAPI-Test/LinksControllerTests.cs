using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Link;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI_Test.Utils;
using System.Net;
using System.Net.Http.Json;

namespace StoreYourStuffAPI_Test
{
    // Extendemos de nuestra querida clase base para tener el _client con el JWT token y la base de datos limpia
    public class LinksControllerTests : BaseIntegrationTest
    {
        public LinksControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

        // GET Obtener todos los links del usuario /api/links
        [Fact]
        public async Task GetAllUserLinks_ReturnsOkYLaLista_CuandoElUsuarioEstaLogueadoYTieneLinks()
        {
            // Arrange
            // Siempre el barrido de base de datos primero para arrancar en blanco
            await ResetDatabaseAsync();
            
            // Nos validamos y guardamos nuestro ID para inyectar registros que sean "nuestros"
            int loggedInUserId = await AuthenticateClientAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Creamos un par de links para nosotros en la base
                db.Links.AddRange(
                    new Link { Title = "Google", Url = "https://google.com", OwnerId = loggedInUserId, IsPrivate = false },
                    new Link { Title = "Secretos", Url = "https://mis-secretos.com", OwnerId = loggedInUserId, IsPrivate = true }
                );
                
                // Y creamos un link ajeno para certificar que efectivamente NO nos lo trae filtrándose bien
                db.Links.Add(new Link { Title = "Link de otro", Url = "https://otro.com", OwnerId = 999, IsPrivate = false });
                
                await db.SaveChangesAsync();
            }

            // Act
            // Invocamos al listado general. No recibe parámetros extra porque el JWT que tiene "_client" hace toda la magia
            var response = await _client.GetAsync("/api/links");

            // Assert
            response.EnsureSuccessStatusCode(); 
            var myLinks = await response.Content.ReadFromJsonAsync<List<LinkPreviewDTO>>();

            // Aquí testeamos dos pájaros de un tiro: que NO traiga links ajenos (solo 2 y no 3), y que vengan nuestros properties limpios
            Assert.NotNull(myLinks);
            Assert.Equal(2, myLinks.Count);
            Assert.Contains(myLinks, l => l.Title == "Secretos" && l.IsPrivate);
        }

        // GET Detalle /api/links/{linkId}
        [Fact]
        public async Task GetLinkDetail_ReturnsForbid_CuandoLinkEsPrivadoYEsDeOtro()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync(); // Hacemos login, somos el usuario principal de la sesión de testing
            long otherUserPrivateLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // El pobre User 999 se armó este link de banco en modo Paranoia (IsPrivate = true)
                var pLink = new Link { Title = "Cosas turbias ajenas", Url = "https://dark.net", OwnerId = 999, IsPrivate = true };
                db.Links.Add(pLink);
                await db.SaveChangesAsync();
                otherUserPrivateLinkId = pLink.Id; // Guardamos el Id generado por EF
            }

            // Act
            // Tratamos de ser curiosos e intentamos consultar el link por ID
            var response = await _client.GetAsync($"/api/links/{otherUserPrivateLinkId}");

            // Assert
            // Aquí el controlador tiene que ser de acero inviolable y clavarnos en la nuca un 403 Forbidden.
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // POST Crear link /api/links
        [Fact]
        public async Task CreateLink_ReturnsCreated_CuandoMandamosDatosValidos()
        {
            // Arrange
            await ResetDatabaseAsync();
            int loggedInUserId = await AuthenticateClientAsync();

            // Armamos nuestro DTO que imitará la carga útil o payload del frontend
            var newLinkDto = new LinkCreateDTO
            {
                Title = "Mi nuevo porfolio",
                Url = "https://yo.dev",
                Description = "Hecho con C# perita",
                IsPrivate = false,
                CategoriesIds = new List<int>() // Ninguna categoría asignada por ahora para hacerlo simple
            };

            // Act
            // Le pegamos al endpoint maestro y despachamos el DTO
            var response = await _client.PostAsJsonAsync("/api/links", newLinkDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var createdLink = await response.Content.ReadFromJsonAsync<LinkResponseDTO>();
            Assert.NotNull(createdLink);
            Assert.Equal("Mi nuevo porfolio", createdLink.Title);
            Assert.Equal(loggedInUserId, createdLink.OwnerId);
            
            // Fíjate que el controlador hace un CreatedAtAction, así que el Identity lo autogeneró y lo devolvió mayor a cero
            Assert.True(createdLink.Id > 0, "Ups, el backend/DB no generó la Primary Key correctamente");
            
            // Verificamos que el header Location esté presente con la URL hacia GetLinkDetail
            Assert.NotNull(response.Headers.Location);
        }

        // POST Compartir link /api/links/{linkId}/share-with/{friendId}
        [Fact]
        public async Task ShareLinkWithFriend_ReturnsNoContent_CuandoEsMiLinkYEsAmigo()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int targetFriendId = 777; // ID del afortunado compi al que se lo vamos a compartir
            long myLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Primero fabricamos el link 100% nuestro que queremos pasarle a él
                var link = new Link { Title = "Receta secreta", Url = "https://comida.com", OwnerId = myUserId, IsPrivate = true };
                db.Links.Add(link);

                // Y por supuesto, para que la API autorice este request, tenemos que ser amigos POSTA en la DB. (Status = 1)
                db.Users.Add(new User { Id = targetFriendId, Alias = "ElAmigo", Email = "amigo@test.com", Password = "123" });
                
                db.Friendships.Add(new Friendship 
                { 
                    RequesterId = myUserId, 
                    AddresseeId = targetFriendId, 
                    Status = 1 // En la lógica del negocio original 1 = Aceptados, 0 = Pending.
                });

                // Tenemos que guardar para que EntityFramework registre todo
                await db.SaveChangesAsync();
                myLinkId = link.Id;
            }

            // Act
            // Le invocamos a la API mandando el POST. Como no recibe Body, pasamos null.
            var response = await _client.PostAsync($"/api/links/{myLinkId}/share-with/{targetFriendId}", null);

            var errorBody = "";
            if (!response.IsSuccessStatusCode)
            {
                errorBody = await response.Content.ReadAsStringAsync();
            }

            // Assert
            // Al ser un endpoint estilo RemoteProcedureCall que solo cruza datos y no devuelve un JSON gordo, devuelve NoContent (204)
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, "Fallo porque dio BadRequest u otro, content: " + errorBody);

            // Obviamente no solo confiamos en el 204. Vamos derecho al metal para revisar la tabla SharedLinks
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Comprobamos si la combinacion perfecta Id+Id asomó en la tabla
                bool esRegistroValido = db.SharedLinks.Any(sl => sl.LinkId == myLinkId && sl.UserId == targetFriendId);
                
                Assert.True(esRegistroValido, "El share no se ha reflejado en la DB, te vendieron humo.");
            }
        }
        // PUT Actualizar link /api/links/{linkId}
        [Fact]
        public async Task UpdateLink_ReturnsNoContent_CuandoModificamosNuestroLink()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            long linkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var link = new Link { OwnerId = myUserId, Title = "Viejo", Url = "viejo.com" };
                db.Links.Add(link);
                await db.SaveChangesAsync();
                linkId = link.Id;
            }

            var updateDto = new LinkUpdateDTO { Title = "Nuevo", Url = "nuevo.com" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/links/{linkId}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Validar en DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedLink = db.Links.Find(linkId);
                Assert.NotNull(updatedLink);
                Assert.Equal("Nuevo", updatedLink.Title);
            }
        }

        [Fact]
        public async Task UpdateLink_ReturnsForbid_SiTratamosDeModificarAlgoAjeno()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            long otherLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var link = new Link { OwnerId = 99, Title = "De otro", Url = "otro.com" };
                db.Links.Add(link);
                await db.SaveChangesAsync();
                otherLinkId = link.Id;
            }

            var updateDto = new LinkUpdateDTO { Title = "Hacked", Url = "hack.com" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/links/{otherLinkId}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // API frena el hackeo
        }

        // DELETE Eliminar link /api/links/{linkId}
        [Fact]
        public async Task DeleteLink_ReturnsNoContentYLoBorra_CuandoEsNuestro()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            long linkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var link = new Link { OwnerId = myUserId, Title = "Byebye", Url = "bye.com" };
                db.Links.Add(link);
                await db.SaveChangesAsync();
                linkId = link.Id;
            }

            // Act
            var response = await _client.DeleteAsync($"/api/links/{linkId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Null(db.Links.Find(linkId));
            }
        }

        // GET Compartidos conmigo /api/links/shared-with-me
        [Fact]
        public async Task GetSharedWithMe_ReturnsOkYLinks_CuandoMisAmigosMeCompartieronCosas()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var friendLink = new Link { OwnerId = 88, Title = "Teclados custom", Url = "keys.com" };
                db.Links.Add(friendLink);
                await db.SaveChangesAsync(); // Para que genere ID

                // Ese amigo me lo comparte
                db.SharedLinks.Add(new SharedLink { LinkId = friendLink.Id, UserId = myUserId });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/links/shared-with-me");

            // Assert
            response.EnsureSuccessStatusCode();
            var sharedLinks = await response.Content.ReadFromJsonAsync<List<LinkPreviewDTO>>();
            
            Assert.NotNull(sharedLinks);
            Assert.Single(sharedLinks);
            Assert.Equal("Teclados custom", sharedLinks[0].Title);
        }

        // DELETE Dejar de compartir /api/links/{linkId}/revoque-share/{friendId}
        [Fact]
        public async Task StopSharingLinkWithFriend_ReturnsNoContent_CuandoQuitamosElAcceso()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int compincheId = 123;
            long myLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var link = new Link { OwnerId = myUserId, Title = "Privado", Url = "priv.com" };
                db.Links.Add(link);
                await db.SaveChangesAsync();
                myLinkId = link.Id;

                db.SharedLinks.Add(new SharedLink { LinkId = myLinkId, UserId = compincheId });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/links/{myLinkId}/revoque-share/{compincheId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                bool sigueCompartido = db.SharedLinks.Any(sl => sl.LinkId == myLinkId && sl.UserId == compincheId);
                Assert.False(sigueCompartido, "La fila debió morir, le revocamos el acceso.");
            }
        }

        // Test extra de contundencia (Edge cases y lógica especial)
        [Fact]
        public async Task GetLinkDetail_ReturnsOk_CuandoElLinkEsDeOtroPeroEsPublico()
        {
            // Arrange
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            long otherUserPublicLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pLink = new Link { Title = "Conocimiento libre", Url = "https://wiki.org", OwnerId = 999, IsPrivate = false };
                db.Links.Add(pLink);
                await db.SaveChangesAsync();
                otherUserPublicLinkId = pLink.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/links/{otherUserPublicLinkId}");

            // Assert
            response.EnsureSuccessStatusCode(); 
            var returnedLink = await response.Content.ReadFromJsonAsync<LinkResponseDTO>();
            Assert.NotNull(returnedLink);
            Assert.Equal("Conocimiento libre", returnedLink.Title);
        }

        [Fact]
        public async Task GetLinkDetail_ReturnsNotFound_CuandoElLinkNoExiste()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var response = await _client.GetAsync($"/api/links/9999999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateLink_ReturnsBadRequest_CuandoUrlEstaFaltante()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            // Falta Url
            var newLinkDto = new LinkCreateDTO { Title = "Sin Url", IsPrivate = false }; 
            var response = await _client.PostAsJsonAsync("/api/links", newLinkDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ShareLinkWithFriend_Falla_CuandoNoSonAmigos()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int unknownGuyId = 888; 
            long myLinkId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var link = new Link { Title = "Privado", Url = "https://priv.com", OwnerId = myUserId, IsPrivate = true };
                db.Links.Add(link);
                db.Users.Add(new User { Id = unknownGuyId, Alias = "Desconocido", Email = "desc@test.com", Password = "123" });
                await db.SaveChangesAsync();
                myLinkId = link.Id;
            }

            // Act
            var response = await _client.PostAsync($"/api/links/{myLinkId}/share-with/{unknownGuyId}", null);

            // Assert
            // No podemos compartir con no amigos
            Assert.False(response.IsSuccessStatusCode, "Se permitió compartir el link a alguien que no es amigo.");
        }
    }
}
