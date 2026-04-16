📦StoreYourStuffAPI
┣ 📂Controllers
┃ ┣ 📜CategoriesController.cs
┃ ┣ 📜FriendshipsController.cs
┃ ┣ 📜LinksController.cs
┃ ┗ 📜UsersController.cs
┣ 📂Data
┃ ┗ 📜AppDbContext.cs
┣ 📂DTOs
┃ ┣ 📂Category
┃ ┃ ┣ 📜CategoryCreateDTO.cs
┃ ┃ ┣ 📜CategoryResponseDTO.cs
┃ ┃ ┗ 📜CategoryUpdateDTO.cs
┃ ┣ 📂Friendship
┃ ┃ ┣ 📜FriendshipCreateDTO.cs
┃ ┃ ┣ 📜FriendshipResponseDTO.cs
┃ ┃ ┗ 📜FriendshipUpdateDTO.cs
┃ ┣ 📂Link
┃ ┃ ┣ 📜LinkCreateDTO.cs
┃ ┃ ┣ 📜LinkPreviewDTO.cs
┃ ┃ ┣ 📜LinkResponseDTO.cs
┃ ┃ ┗ 📜LinkUpdateDTO.cs
┃ ┣ 📂LinkCategories
┃ ┣ 📂SharedLinks
┃ ┃ ┗ 📜ShareLinkRequestDTO.cs
┃ ┗ 📂User
┃ ┃ ┣ 📜LoginDTO.cs
┃ ┃ ┣ 📜UserCreateDTO.cs
┃ ┃ ┣ 📜UserPreviewDTO.cs
┃ ┃ ┣ 📜UserResponseDTO.cs
┃ ┃ ┗ 📜UserUpdateDTO.cs
┣ 📂Extensions
┃ ┗ 📜ClaimsPrincipalExtensions.cs
┣ 📂Models
┃ ┣ 📜Category.cs
┃ ┣ 📜Friendship.cs
┃ ┣ 📜Link.cs
┃ ┣ 📜LinkCategory.cs
┃ ┣ 📜SharedLink.cs
┃ ┗ 📜User.cs
┣ 📂Properties
┃ ┗ 📜launchSettings.json
┣ 📂Security
┃ ┣ 📜Argon2PasswordHasher.cs
┃ ┣ 📜IPasswordHasher.cs
┃ ┣ 📜ITokenService.cs
┃ ┗ 📜TokenService.cs
┣ 📜appsettings.Development.json
┣ 📜appsettings.json
┣ 📜Program.cs
┣ 📜StoreYourStuffAPI.csproj
┣ 📜StoreYourStuffAPI.csproj.user
┣ 📜StoreYourStuffAPI.http
┗ 📜StoreYourStuffAPI.sln
