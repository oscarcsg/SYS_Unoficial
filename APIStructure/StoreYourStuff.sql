-- StoreYourStuff

-- --------------------------- --
--            TABLES           --
-- --------------------------- --
IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
	CREATE TABLE Users (
		userId INT IDENTITY(1, 1) PRIMARY KEY,
		alias VARCHAR(20) NOT NULL UNIQUE,
		email VARCHAR(321) NOT NULL UNIQUE CHECK (email LIKE '%@%'),
		password VARCHAR(255) NOT NULL,
		createdAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
		lastSignIn DATETIME2 DEFAULT NULL
	) WITH (DATA_COMPRESSION = ROW);
END

IF OBJECT_ID('Friendships', 'U') IS NULL
BEGIN
	CREATE TABLE Friendships (
		requesterId INT NOT NULL,
		addresseeId INT NOT NULL,
		-- status: 0 = pending, 1 = accepted, 2 = declined, 3 = blocked
		status TINYINT NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2, 3)),
		createdAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

		-- Columnas calculadas para evitar solicitudes duplicadas inversas (Ej: 1-2 y 2-1)
		_userMin AS (CASE WHEN requesterId < addresseeId THEN requesterId ELSE addresseeId END) PERSISTED,
		_userMax AS (CASE WHEN requesterId < addresseeId THEN addresseeId ELSE requesterId END) PERSISTED,

		-- PK compuesta
		CONSTRAINT PK_Friendships PRIMARY KEY (requesterId, addresseeId),
		-- FKs
		CONSTRAINT FK_Friendships_Requester FOREIGN KEY (requesterId) REFERENCES Users(userId),
		CONSTRAINT FK_Friendships_Addressee FOREIGN KEY (addresseeId) REFERENCES Users(userId),
		-- Evita que el usuario se agregue a sí mismo
		CONSTRAINT CHK_Friendships_NoSelf CHECK (requesterId <> addresseeId),
		-- Restricción de unicidad usando las columnas calculadas
		CONSTRAINT UQ_Friendships_Bidirectional UNIQUE (_userMin, _userMax)
	) WITH (DATA_COMPRESSION = ROW);

	-- Índice para buscar rápido quién te ha enviado solicitudes
	CREATE NONCLUSTERED INDEX IX_Friendships_AddresseeId ON Friendships(addresseeId);
END

IF OBJECT_ID('Links', 'U') IS NULL
BEGIN
	CREATE TABLE Links (
		linkId BIGINT IDENTITY(1, 1) PRIMARY KEY,
		title NVARCHAR(100) NOT NULL,
		description NVARCHAR(1000),
		url VARCHAR(500) NOT NULL,
		isPrivate BIT NOT NULL DEFAULT 0,
		ownerId INT NOT NULL,
		createdAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

		CONSTRAINT FK_Links_Owner FOREIGN KEY (ownerId) REFERENCES Users(userId)
	) WITH (DATA_COMPRESSION = ROW);

	-- Crea un índice súper rápido para buscar links por su dueño
	CREATE NONCLUSTERED INDEX IX_Links_OwnerId ON Links(ownerId);
END

IF OBJECT_ID('Categories', 'U') IS NULL
BEGIN
	CREATE TABLE Categories (
		categoryId INT IDENTITY(1, 1) PRIMARY KEY,
		name NVARCHAR(50) NOT NULL,
		hexColor CHAR(6) NOT NULL DEFAULT 'd2d2d2',
		isPrivate BIT NOT NULL DEFAULT 0, -- Si es 1, entonces esa categoría NO se compartirá con otros usuarios en enlaces compartidos
		ownerId INT DEFAULT NULL,

		CONSTRAINT FK_Categories_Owner FOREIGN KEY (ownerId) REFERENCES Users(userId)
	) WITH (DATA_COMPRESSION = ROW);
	
	CREATE NONCLUSTERED INDEX IX_Categories_OwnerId ON Categories(ownerId);
END
-- NOTA: Las categorías tienen dueño, las categorías del sistema (las predefinidas para todos los usuarios) tiene como owner "NULL"
-- las categorías que tengan como owner el id del usuario que las creó, sólo ese y sus compartidos (en caso de que la categoría no
-- esté marcada como privada) podrán ver la categoría.

IF OBJECT_ID('LinkCategories', 'U') IS NULL
BEGIN
	CREATE TABLE LinkCategories (
		linkId BIGINT NOT NULL,
		categoryId INT NOT NULL,

		-- PK compuesta
		CONSTRAINT PK_LinkCategories PRIMARY KEY (linkId, categoryId),
		-- FKs
		CONSTRAINT FK_LinkCategories_Links FOREIGN KEY (linkId) REFERENCES Links(linkId),
		CONSTRAINT FK_LinkCategories_Categories FOREIGN KEY (categoryId) REFERENCES Categories(categoryId)
	) WITH (DATA_COMPRESSION = ROW);
END

IF OBJECT_ID('SharedLinks', 'U') IS NULL
BEGIN
	CREATE TABLE SharedLinks (
		linkId BIGINT NOT NULL,
		userId INT NOT NULL,
		sharedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

		-- PK compuesta
		CONSTRAINT PK_SharedLinks PRIMARY KEY (linkId, userId),
		-- FKs
		CONSTRAINT FK_SharedLinks_Links FOREIGN KEY (linkId) REFERENCES Links(linkId),
		CONSTRAINT FK_SharedLinks_Users FOREIGN KEY (userId) REFERENCES Users(userId)
	) WITH (DATA_COMPRESSION = ROW);

	-- Crea un índice para buscar rápidamente con qué usuarios se compartió un link
	CREATE NONCLUSTERED INDEX IX_SharedLinks_UserId ON SharedLinks(userId);
END


-- --------------------------- --
--           INSERTS           --
-- --------------------------- --
-- 1. USUARIOS
INSERT INTO Users (alias, email, password)
VALUES 
    ('oscar', 'oscar@syt.com', '12345'), -- userId = 1
    ('maria', 'maria@syt.com', '54321'); -- userId = 2

-- 1.5 AMISTADES (NUEVO)
INSERT INTO Friendships (requesterId, addresseeId, status)
VALUES 
    (1, 2, 1); -- Oscar (1) le envió a Maria (2) y ella Aceptó (1)

-- 2. CATEGORÍAS
INSERT INTO Categories (name, hexColor, isPrivate, ownerId)
VALUES 
    -- Categorías del sistema (ownerId es NULL, private es 0)
    ('Programación', '2ECC71', 0, NULL),     -- categoryId = 1
    ('Herramientas', 'E67E22', 0, NULL),     -- categoryId = 2
    ('Noticias', '3498DB', 0, NULL),         -- categoryId = 3
    ('Entretenimiento', '9B59B6', 0, NULL),  -- categoryId = 4
    
    -- Categoría personalizada de Oscar y PRIVADA
    ('Proyectos Secretos', 'E74C3C', 1, 1);  -- categoryId = 5

-- 3. ENLACES (LINKS)
INSERT INTO Links (title, description, url, isPrivate, ownerId)
VALUES 
    ('GitHub', 'Plataforma para alojar repositorios Git', 'https://github.com', 1, 1),             -- linkId = 1
    ('StackOverflow', 'Foro de preguntas y respuestas', 'https://stackoverflow.com', 0, 1),        -- linkId = 2
    ('Hacker News', 'Noticias sobre tecnología y startups', 'https://news.ycombinator.com', 0, 1), -- linkId = 3
    ('Regex101', 'Prueba y testeo de expresiones regulares', 'https://regex101.com', 0, 1),        -- linkId = 4
    ('YouTube', 'Plataforma de videos', 'https://youtube.com', 0, 1);                              -- linkId = 5

-- 4. CATEGORÍAS DE LOS ENLACES
INSERT INTO LinkCategories (linkId, categoryId)
VALUES 
    (1, 1), -- GitHub -> Programación
    (1, 2), -- GitHub -> Herramientas
    (1, 5), -- GitHub -> Proyectos Secretos (Categoría privada de Oscar)
    (2, 1), -- StackOverflow -> Programación
    (3, 3), -- Hacker News -> Noticias
    (4, 1), -- Regex101 -> Programación
    (4, 2), -- Regex101 -> Herramientas
    (5, 4); -- YouTube -> Entretenimiento

-- 5. ENLACES COMPARTIDOS
INSERT INTO SharedLinks (linkId, userId)
VALUES 
    (1, 2); -- El link 1 (GitHub) se comparte con el usuario 2 (Maria)