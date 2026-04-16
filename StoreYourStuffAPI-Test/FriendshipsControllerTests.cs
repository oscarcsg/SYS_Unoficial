using Microsoft.Extensions.DependencyInjection;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Friendship;
using StoreYourStuffAPI.DTOs.User;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI_Test.Utils;
using System.Net;
using System.Net.Http.Json;

namespace StoreYourStuffAPI_Test
{
    // Extendemos de nuestra querida clase base para tener _client con la autenticación precargada
    public class FriendshipsControllerTests : BaseIntegrationTest
    {
        public FriendshipsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

        // GET Obtener lista de amigos aprobados /api/friendships
        [Fact]
        public async Task GetFriends_ReturnsOkYLaLista_CuandoTenemosAmigosAceptados()
        {
            // Arrange
            // Siempre a resetear la DB para arrancar con el escritorio limpio
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int friend1Id = 100;
            int friend2Id = 200;
            int unknownId = 300;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Primero metemos un par de usuarios extra para que EntityFramework no patalee cuando se inyecten las Foráneas de Friendship
                db.Users.AddRange(
                    new User { Id = friend1Id, Alias = "Amigo1", Email = "a1@test.com", Password = "123" },
                    new User { Id = friend2Id, Alias = "Amigo2", Email = "a2@test.com", Password = "123" },
                    new User { Id = unknownId, Alias = "Desconocido", Email = "u@test.com", Password = "123" }
                );

                // Armamos las amistades bidireccionales
                db.Friendships.AddRange(
                    new Friendship { RequesterId = myUserId, AddresseeId = friend1Id, Status = 1 }, // Yo le rogué, él aceptó (Amigos posta)
                    new Friendship { RequesterId = friend2Id, AddresseeId = myUserId, Status = 1 }, // Él me rogó, yo acepté (Amigos posta)
                    new Friendship { RequesterId = myUserId, AddresseeId = unknownId, Status = 0 }  // Sigue pendiente en el limbo, NO debe salir en amigos
                );

                await db.SaveChangesAsync();
            }

            // Act
            // Pedimos los datos tirando al GET pelado
            var response = await _client.GetAsync("/api/friendships");

            // Assert
            response.EnsureSuccessStatusCode();
            var myFriends = await response.Content.ReadFromJsonAsync<List<UserPreviewDTO>>();

            // Magia: la query debe saber discernir entre quien lo mandó y hacer la intersección para traerme sus previews
            Assert.NotNull(myFriends);
            Assert.Equal(2, myFriends.Count);
            
            // Comprobamos expresamente que el desconocido no se nos ha colado en la fiesta
            Assert.DoesNotContain(myFriends, f => f.Alias == "Desconocido");
        }

        // POST Enviar solicitud de amistad /api/friendships/request/{addresseeId}
        [Fact]
        public async Task SendFriendRequest_ReturnsOk_CuandoUsuarioExisteYNoEranAmigos()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int targetUserId = 404; // User al que le vamos a suplicar amistad

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = targetUserId, Alias = "FuturoAmigo", Email = "future@test.com", Password = "123" });
                await db.SaveChangesAsync();
            }

            // Act
            // Inyectamos el POST sin body, porque el target Id se arma en la query URL
            var response = await _client.PostAsync($"/api/friendships/request/{targetUserId}", null);

            // Assert
            // El controlador espera respondernos validamente con un 200 y escupiendo info del otro pavo (UserPreviewDTO)
            response.EnsureSuccessStatusCode(); 
            
            var returnedTarget = await response.Content.ReadFromJsonAsync<UserPreviewDTO>();
            Assert.NotNull(returnedTarget);
            Assert.Equal("FuturoAmigo", returnedTarget.Alias);

            // Vamos también al barro de la Base de Datos para asegurar que no solo dio Ok, sino que grabó un 0 en Status (Pending).
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var friendship = db.Friendships.FirstOrDefault(f => f.RequesterId == myUserId && f.AddresseeId == targetUserId);
                
                Assert.NotNull(friendship);
                Assert.Equal(0, friendship.Status);
            }
        }

        // PUT Aceptar solicitud /api/friendships/respond/{requesterId}
        [Fact]
        public async Task UpdateFriendship_ReturnsNoContent_CuandoAceptamosSolicitud()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int pesaoId = 99; // Usuario pesado que nos ha mandado solicitud previa

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = pesaoId, Alias = "Pesao", Email = "pesao@test.com", Password = "123" });
                
                // En este contexto previo a que decidamos algo, él es el "Requester" y nosotros el "Addressee". Status 0 por defecto.
                db.Friendships.Add(new Friendship { RequesterId = pesaoId, AddresseeId = myUserId, Status = 0 });
                await db.SaveChangesAsync();
            }

            // Preparamos nuestro veredicto: aceptamos dándole un '1'
            var updateDto = new FriendshipUpdateDTO { Status = 1 };

            // Act
            // Le encajamos el Update a la API
            var response = await _client.PutAsJsonAsync($"/api/friendships/respond/{pesaoId}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Revisamos bien el update del chabón
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var friendship = db.Friendships.FirstOrDefault(f => f.RequesterId == pesaoId && f.AddresseeId == myUserId);
                
                Assert.NotNull(friendship);
                Assert.Equal(1, friendship.Status); // Oficialmente panas!
            }
        }

        // PUT Rechazar solicitud /api/friendships/respond/{requesterId}
        [Fact]
        public async Task UpdateFriendship_RemovesRelation_CuandoRechazamosSolicitud()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int haterId = 13;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = haterId, Alias = "Hater", Email = "hater@test.com", Password = "123" });
                
                // Status pendiente
                db.Friendships.Add(new Friendship { RequesterId = haterId, AddresseeId = myUserId, Status = 0 });
                await db.SaveChangesAsync();
            }

            // Status 2 es "Rechazado". Como sabemos por el controller, hay una lógica letal y justiciera para este número específico.
            var updateDto = new FriendshipUpdateDTO { Status = 2 };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/friendships/respond/{haterId}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Vamos al metal de vuelta: El controlador no deja el status en 2 estúpidamente, sino que aplica:
            // "if (updateData.Status == 2) _context.Friendships.Remove(friendship);" borrándolo.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var friendship = db.Friendships.FirstOrDefault(f => f.RequesterId == haterId && f.AddresseeId == myUserId);
                
                // Comprobamos que el registro palmó definitivamente, como debe ser.
                Assert.Null(friendship); 
            }
        }
        // GET Pendientes que recibi /api/friendships/pending/recieved
        [Fact]
        public async Task GetPendingRecieved_ReturnsOk_SolamenteConLasQueMeMandaronAmi()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = 81, Alias = "Admirador", Email = "adm@t.com", Password = "123" });
                
                // Me manda a mi (Status = 0)
                db.Friendships.Add(new Friendship { RequesterId = 81, AddresseeId = myUserId, Status = 0 });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/friendships/pending/recieved");

            // Assert
            response.EnsureSuccessStatusCode();
            var pending = await response.Content.ReadFromJsonAsync<List<UserPreviewDTO>>();
            Assert.NotNull(pending);
            Assert.Single(pending);
            Assert.Equal("Admirador", pending[0].Alias);
        }

        // GET Pendientes que yo mande /api/friendships/pending/sent
        [Fact]
        public async Task GetPendingSent_ReturnsOk_SolamenteConLasQueYoPropuse()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = 82, Alias = "Crush", Email = "crush@t.com", Password = "123" });
                
                // Yo se lo mando a ella (Status = 0)
                db.Friendships.Add(new Friendship { RequesterId = myUserId, AddresseeId = 82, Status = 0 });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/friendships/pending/sent");

            // Assert
            response.EnsureSuccessStatusCode();
            var pending = await response.Content.ReadFromJsonAsync<List<UserPreviewDTO>>();
            Assert.NotNull(pending);
            Assert.Single(pending);
            Assert.Equal("Crush", pending[0].Alias);
        }

        // DELETE Quitar amigo /api/friendships/remove/{friendId}
        [Fact]
        public async Task RemoveFriend_ReturnsNoContent_CuandoBorroAMiAmigo()
        {
            // Arrange
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int exFriendId = 77;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = exFriendId, Alias = "ExAmigo", Email = "ex@t.com", Password = "123" });
                
                // Ambos somos compadres (Status 1)
                db.Friendships.Add(new Friendship { RequesterId = myUserId, AddresseeId = exFriendId, Status = 1 });
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/friendships/remove/{exFriendId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Revisamos en DB si la amistad palmó de verdad
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var relationShip = db.Friendships.FirstOrDefault(f => 
                    (f.RequesterId == myUserId && f.AddresseeId == exFriendId) ||
                    (f.RequesterId == exFriendId && f.AddresseeId == myUserId)
                );
                
                Assert.Null(relationShip); // Ya son desconocidos
            }
        }

        // Edge cases y robustez (Contundencia)
        [Fact]
        public async Task SendFriendRequest_ReturnsBadRequest_CuandoNosEnviamosANosotrosMismos()
        {
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            
            var response = await _client.PostAsync($"/api/friendships/request/{myUserId}", null);
            Assert.False(response.IsSuccessStatusCode, "El sistema permitió autodeclararnos amigos. Debe fallar.");
        }

        [Fact]
        public async Task SendFriendRequest_ReturnsConflictOBadRequest_CuandoYaExisteSolicitud()
        {
            await ResetDatabaseAsync();
            int myUserId = await AuthenticateClientAsync();
            int targetUserId = 404;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Add(new User { Id = targetUserId, Alias = "Target", Email = "t@test.com", Password = "123" });
                // Ya existe solicitud
                db.Friendships.Add(new Friendship { RequesterId = myUserId, AddresseeId = targetUserId, Status = 0 });
                await db.SaveChangesAsync();
            }

            var response = await _client.PostAsync($"/api/friendships/request/{targetUserId}", null);
            Assert.False(response.IsSuccessStatusCode, "Permitió mandar solicitud duplicada, debería fallar.");
        }

        [Fact]
        public async Task UpdateFriendship_ReturnsNotFound_CuandoRespondemosASolicitudInexistente()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            var updateDto = new FriendshipUpdateDTO { Status = 1 };
            
            var response = await _client.PutAsJsonAsync($"/api/friendships/respond/99999", updateDto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveFriend_ReturnsNotFound_CuandoNoExisteLaAmistadOBorramosDesconocido()
        {
            await ResetDatabaseAsync();
            await AuthenticateClientAsync();
            
            var response = await _client.DeleteAsync($"/api/friendships/remove/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
