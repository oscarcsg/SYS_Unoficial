using Bogus;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI.Security;

namespace StoreYourStuffAPI_Test.Utils
{
    public static class TestDataSeeder
    {
        public const string DEFAULT_PASSWORD = "123456";
        private const string DEFAULT_HASH = "239p/xhacOhZo91DXsoFsw==:RoeoyuQ/8+Hnudob4bIV/WfrJTtCOR6KYDphdMTsv6w=";

        static TestDataSeeder()
        {
            // Static seed so the data will be the 
            Randomizer.Seed = new Random(123456);
        }

        // Método para generar usuarios
        public static List<User> GenerateUsers(int count)
        {
            return new Faker<User>("es")
                .RuleFor(u => u.Alias, f => f.Internet.UserName().Length > 20 ? f.Internet.UserName()[..20] : f.Internet.UserName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Alias))
                .RuleFor(u => u.Password, f => DEFAULT_HASH)
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(1))
                .RuleFor(u => u.LastSignIn, f => f.Date.Recent())
                .Generate(count);
        }

        public static List<Category> GenerateSystemCategories()
        {
            // Categorías predefinidas (OwnerId = null)
            var names = new[] { "Programación", "Herramientas", "Noticias", "Entretenimiento" };
            var faker = new Faker();

            return names.Select(name => new Category
            {
                Name = name,
                HexColor = faker.Random.Hexadecimal(6, "").ToLower(), // Genera sin el '#'
                IsPrivate = false,
                OwnerId = null
            }).ToList();
        }

        // --- NIVEL 2: Entidades Dependientes ---

        public static List<Category> GenerateCustomCategories(int count, List<User> existingUsers)
        {
            return new Faker<Category>("es")
                .RuleFor(c => c.Name, f => f.Commerce.Categories(1)[0])
                .RuleFor(c => c.HexColor, f => f.Random.Hexadecimal(6, "").ToLower())
                .RuleFor(c => c.IsPrivate, f => f.Random.Bool())
                .RuleFor(c => c.OwnerId, f => f.PickRandom(existingUsers).Id)
                .Generate(count);
        }

        public static List<Link> GenerateLinks(int count, List<User> existingUsers)
        {
            return new Faker<Link>("es")
                .RuleFor(l => l.Title, f => f.Lorem.Sentence(3))
                .RuleFor(l => l.Description, f => f.Lorem.Paragraph())
                .RuleFor(l => l.Url, f => f.Internet.Url())
                .RuleFor(l => l.IsPrivate, f => f.Random.Bool(0.2f))
                .RuleFor(l => l.OwnerId, f => f.PickRandom(existingUsers).Id)
                .RuleFor(l => l.CreatedAt, f => f.Date.Past())
                .Generate(count);
        }

        // --- NIVEL 3: Tablas Puente (Relaciones) ---

        public static List<Friendship> GenerateFriendships(List<User> users, int numberOfFriendships)
        {
            var friendships = new List<Friendship>();
            var faker = new Faker();

            // Para evitar duplicados, intentamos emparejar hasta conseguir el número deseado
            int attempts = 0;
            while (friendships.Count < numberOfFriendships && attempts < numberOfFriendships * 10)
            {
                var user1 = faker.PickRandom(users);
                var user2 = faker.PickRandom(users);

                if (user1.Id != user2.Id)
                {
                    // Comprobamos que no exista ya esa amistad (en ninguna dirección)
                    bool exists = friendships.Any(f =>
                        (f.RequesterId == user1.Id && f.AddresseeId == user2.Id) ||
                        (f.RequesterId == user2.Id && f.AddresseeId == user1.Id));

                    if (!exists)
                    {
                        friendships.Add(new Friendship
                        {
                            RequesterId = user1.Id,
                            AddresseeId = user2.Id,
                            Status = faker.Random.Byte(0, 3) // 0=pending, 1=accepted, 2=declined, 3=blocked
                        });
                    }
                }
                attempts++;
            }
            return friendships;
        }

        public static List<LinkCategory> AssignCategoriesToLinks(List<Link> links, List<Category> categories)
        {
            var linkCategories = new List<LinkCategory>();
            var faker = new Faker();

            foreach (var link in links)
            {
                // Asignamos entre 1 y 3 categorías por cada link
                var categoriesForThisLink = faker.PickRandom(categories, faker.Random.Int(1, 3));

                foreach (var cat in categoriesForThisLink)
                {
                    linkCategories.Add(new LinkCategory
                    {
                        LinkId = link.Id,
                        CategoryId = cat.Id
                    });
                }
            }
            return linkCategories;
        }
    }
}
