import random
import string
import sys
from datetime import datetime, timedelta
from faker import Faker

# Inicializar Faker con múltiples lenguas (alfabeto latino)
fake = Faker(['es_ES', 'en_US', 'it_IT', 'fr_FR', 'de_DE', 'pt_PT'])

# ==========================================
# CONFIGURACIÓN (PUNTOS DE CONTROL)
# ==========================================
NUM_USERS = 1500

# Rango de links por usuario
MIN_PUBLIC_LINKS_PER_USER = 18
MAX_PUBLIC_LINKS_PER_USER = 32
MIN_PRIVATE_LINKS_PER_USER = 5
MAX_PRIVATE_LINKS_PER_USER = 10

# Rango de categorías por usuario
MIN_PUBLIC_CATEGORIES_PER_USER = 4
MAX_PUBLIC_CATEGORIES_PER_USER = 12
MIN_PRIVATE_CATEGORIES_PER_USER = 2
MAX_PRIVATE_CATEGORIES_PER_USER = 8

# Probabilidades de relaciones (por usuario)
AVG_FRIENDS_PER_USER = 10
AVG_PENDING_REQUESTS_PER_USER = 6
AVG_REJECTIONS_PER_USER = 2
AVG_BLOCKS_PER_USER = 4

# Links compartidos por cada amistad aceptada
MIN_SHARED_LINKS_PER_FRIEND = 3
MAX_SHARED_LINKS_PER_FRIEND = 6

# IDs iniciales para no solaparse con los inserts manuales
START_USER_ID = 1
START_CATEGORY_ID = 1
START_LINK_ID = 1

# ==========================================
# ESTIMACIÓN DE DATOS
# ==========================================
avg_links = ((MIN_PUBLIC_LINKS_PER_USER + MAX_PUBLIC_LINKS_PER_USER) / 2) + ((MIN_PRIVATE_LINKS_PER_USER + MAX_PRIVATE_LINKS_PER_USER) / 2)
avg_cats = ((MIN_PUBLIC_CATEGORIES_PER_USER + MAX_PUBLIC_CATEGORIES_PER_USER) / 2) + ((MIN_PRIVATE_CATEGORIES_PER_USER + MAX_PRIVATE_CATEGORIES_PER_USER) / 2)
total_links = int(NUM_USERS * avg_links)
total_cats = int(NUM_USERS * avg_cats)
total_friendships = int(NUM_USERS * (AVG_FRIENDS_PER_USER + AVG_PENDING_REQUESTS_PER_USER + AVG_REJECTIONS_PER_USER + AVG_BLOCKS_PER_USER))
total_shared = int((NUM_USERS * (AVG_FRIENDS_PER_USER / 2)) * ((MIN_SHARED_LINKS_PER_FRIEND + MAX_SHARED_LINKS_PER_FRIEND) / 2))

print("=== ESTIMACIÓN DE GENERACIÓN ===")
print(f"Usuarios: {NUM_USERS}")
print(f"Colecciones (Categorías): ~{total_cats}")
print(f"Enlaces (Links): ~{total_links}")
print(f"Relaciones (Amistades/Bloqueos): ~{total_friendships}")
print(f"Enlaces compartidos: ~{total_shared}")
print(f"Total aproximado de sentencias INSERT: {NUM_USERS + total_cats + total_links + total_friendships + total_shared + (total_links * 2)}")
print("================================")

confirmacion = input("¿Deseas proceder con la generación de los datos? (s/n): ").lower()
if confirmacion != 's':
    print("Generación cancelada por el usuario.")
    sys.exit()

# ==========================================
# DATOS DE APOYO
# ==========================================
# No se necesitan listas cerradas de nombres ahora que usamos Faker
# Pero mantenemos algunas categorías base para guiar la generación
TOPICS = ["Programación", "Diseño", "Salud", "Deportes", "Cocina", "Viajes", "Finanzas", "Música", "Juegos", "IA", "Ciencia", "Historia", "Arte", "Cine", "Fotografía", "Jardinería", "Moda", "Mascotas", "Espacio", "Educación", "Tecnología", "Blockchain", "Cloud", "Ciberseguridad", "Marketing"]

def get_random_color():
    return ''.join(random.choice('0123456789ABCDEF') for _ in range(6))

def get_random_date():
    start_date = datetime(2024, 1, 1)
    end_date = datetime.now()
    delta = end_date - start_date
    int_delta = (delta.days * 24 * 60 * 60) + delta.seconds
    random_second = random.randrange(int_delta)
    return start_date + timedelta(seconds=random_second)

def format_sql_val(val, is_unicode=False):
    if val is None: return "NULL"
    if isinstance(val, bool): return "1" if val else "0"
    if isinstance(val, (int, float)): return str(val)
    if isinstance(val, datetime): return f"'{val.isoformat()}'"
    escaped = str(val).replace("'", "''")
    prefix = "N" if is_unicode else ""
    return f"{prefix}'{escaped}'"

def write_bulk_insert(f, table_name, columns, rows, unicode_mask=None, chunk_size=500):
    if not rows: return
    for i in range(0, len(rows), chunk_size):
        chunk = rows[i:i + chunk_size]
        f.write(f"INSERT INTO {table_name} ({', '.join(columns)})\nVALUES\n")
        for j, row in enumerate(chunk):
            vals = [format_sql_val(v, unicode_mask[idx] if unicode_mask else False) for idx, v in enumerate(row)]
            line = f"    ({', '.join(vals)})"
            f.write(line + (",\n" if j < len(chunk) - 1 else ";\n\n"))

# ==========================================
# GENERACIÓN
# ==========================================

users = []
categories = []
links = []
link_categories = []
friendships = []
shared_links = []

# 1. Generar Usuarios
used_aliases = set()
DEFAULT_PASSWORD_HASH = "239p/xhacOhZo91DXsoFsw==:RoeoyuQ/8+Hnudob4bIV/WfrJTtCOR6KYDphdMTsv6w="

for i in range(NUM_USERS):
    uid = START_USER_ID + i
    
    # Generar alias único (máx 20 caracteres)
    while True:
        # Faker user_name a veces es largo o tiene puntos
        raw_alias = fake.user_name()[:20]
        # Limpiar caracteres que no nos gusten si es necesario, pero user_name suele ser ok
        if raw_alias.lower() not in used_aliases:
            alias = raw_alias
            used_aliases.add(alias.lower())
            break
            
    # Generar email único basado en el alias (máx 321 caracteres)
    # email = f"{alias}@{fake.free_email_domain()}"
    email = fake.email() # Faker ya garantiza un formato válido
    # Asegurarnos de que el email también sea único en nuestro set (por si acaso)
    email_parts = email.split('@')
    email = f"{alias}@{email_parts[1]}" # Forzamos que use el alias para cumplir el deseo del usuario
    
    password = DEFAULT_PASSWORD_HASH
    created_at = get_random_date()
    users.append((uid, alias, email, password, created_at))

# 2. Generar Categorías y Links por Usuario
current_cat_id = START_CATEGORY_ID
current_link_id = START_LINK_ID

for uid_data in users:
    uid = uid_data[0]
    
    # Categorías del usuario
    num_pub_cats = random.randint(MIN_PUBLIC_CATEGORIES_PER_USER, MAX_PUBLIC_CATEGORIES_PER_USER)
    num_priv_cats = random.randint(MIN_PRIVATE_CATEGORIES_PER_USER, MAX_PRIVATE_CATEGORIES_PER_USER)
    
    user_cats = []
    # Públicas
    for _ in range(num_pub_cats):
        # Usamos palabras en diferentes idiomas gracias a Faker
        cat_name = fake.word().capitalize()
        if random.random() > 0.5:
            cat_name = f"{cat_name} {fake.word().capitalize()}"
        
        # Limitar longitud si fuera necesario (NVARCHAR(50))
        cat_name = cat_name[:50]
        
        categories.append((current_cat_id, cat_name, get_random_color(), 0, uid))
        user_cats.append(current_cat_id)
        current_cat_id += 1
    # Privadas
    for _ in range(num_priv_cats):
        cat_name = f"Private {fake.word().capitalize()}"[:50]
        categories.append((current_cat_id, cat_name, get_random_color(), 1, uid))
        user_cats.append(current_cat_id)
        current_cat_id += 1
        
    # Links del usuario
    num_pub_links = random.randint(MIN_PUBLIC_LINKS_PER_USER, MAX_PUBLIC_LINKS_PER_USER)
    num_priv_links = random.randint(MIN_PRIVATE_LINKS_PER_USER, MAX_PRIVATE_LINKS_PER_USER)
    
    for (num, is_private) in [(num_pub_links, 0), (num_priv_links, 1)]:
        for _ in range(num):
            # Título y descripción realistas con Faker
            title = fake.sentence(nb_words=random.randint(2, 5))[:100]
            desc = fake.paragraph(nb_sentences=random.randint(1, 3))[:1000]
            
            # URL realista
            url = fake.url()
            created_at = get_random_date()
            links.append((current_link_id, title, desc, url, is_private, uid, created_at))
            
            # Asignar a 1-3 categorías
            # Nota: Si START_CATEGORY_ID es 1, asumimos que se han borrado las del sistema o se están recreando
            potential_cats = user_cats
            if not potential_cats: 
                # Fallback por si acaso
                potential_cats = [1, 2, 3, 4]

            num_cats_to_assign = random.randint(1, 3)
            assigned = random.sample(potential_cats, min(len(potential_cats), num_cats_to_assign))
            for cid in assigned:
                link_categories.append((current_link_id, cid))
            
            current_link_id += 1

# 3. Amistades y Relaciones
existing_friendships = set()

def add_friendship(u1, u2, status):
    pair = tuple(sorted((u1, u2)))
    if pair not in existing_friendships and u1 != u2:
        existing_friendships.add(pair)
        friendships.append((u1, u2, status, get_random_date()))
        return True
    return False

uids = [u[0] for u in users]

for uid in uids:
    # Aceptadas (status 1)
    for _ in range(AVG_FRIENDS_PER_USER // 2):
        other = random.choice(uids)
        add_friendship(uid, other, 1)
    
    # Pendientes (status 0)
    for _ in range(AVG_PENDING_REQUESTS_PER_USER):
        other = random.choice(uids)
        add_friendship(uid, other, 0)
        
    # Rechazadas (status 2)
    for _ in range(AVG_REJECTIONS_PER_USER):
        other = random.choice(uids)
        add_friendship(uid, other, 2)
        
    # Bloqueos (status 3)
    for _ in range(AVG_BLOCKS_PER_USER):
        other = random.choice(uids)
        add_friendship(uid, other, 3)

# 4. Enlaces Compartidos
friendships_accepted = [f for f in friendships if f[2] == 1]
for f in friendships_accepted:
    req, add, status, date = f
    owner_links = [l[0] for l in links if l[5] == req]
    if owner_links:
        num_to_share = random.randint(MIN_SHARED_LINKS_PER_FRIEND, MAX_SHARED_LINKS_PER_FRIEND)
        to_share = random.sample(owner_links, min(len(owner_links), num_to_share))
        for lid in to_share:
            shared_links.append((lid, add))

# ==========================================
# ESCRITURA DEL ARCHIVO SQL
# ==========================================
filename = "StoreYourStuff-MockData.sql"
print(f"Generando {filename}...")

with open(filename, "w", encoding="utf-8") as f:
    f.write("-- Mock Data Generada Automáticamente\n")
    f.write("-- StoreYourStuff\n\n")
    
    f.write("-- 0. LIMPIEZA DE TABLAS (Orden correcto para FKs)\n")
    f.write("DELETE FROM SharedLinks;\n")
    f.write("DELETE FROM LinkCategories;\n")
    f.write("DELETE FROM Friendships;\n")
    f.write("DELETE FROM Links;\n")
    f.write("DELETE FROM Categories;\n")
    f.write("DELETE FROM Users;\n")
    f.write("GO\n\n")
    
    # Reiniciar seeds de identidad (Opcional, pero recomendado si START_ID = 1)
    f.write("-- Reiniciar contadores de identidad\n")
    f.write("DBCC CHECKIDENT ('Users', RESEED, 0);\n")
    f.write("DBCC CHECKIDENT ('Categories', RESEED, 0);\n")
    f.write("DBCC CHECKIDENT ('Links', RESEED, 0);\n")
    f.write("GO\n\n")

    # Usuarios
    f.write("-- 1. USUARIOS\n")
    # u = (uid, alias, email, password, created_at) -> No incluimos uid por ser IDENTITY
    user_rows = [u[1:] for u in users]
    write_bulk_insert(f, "Users", ["alias", "email", "password", "createdAt"], user_rows)
    
    # Categorías
    f.write("-- 2. CATEGORÍAS\n")
    # c = (current_cat_id, cat_name, get_random_color(), isPrivate, uid) -> No incluimos id
    cat_rows = [c[1:] for c in categories]
    write_bulk_insert(f, "Categories", ["name", "hexColor", "isPrivate", "ownerId"], cat_rows, unicode_mask=[True, False, False, False])

    # Links
    f.write("-- 3. ENLACES (LINKS)\n")
    # l = (current_link_id, title, desc, url, is_private, uid, created_at) -> No incluimos id
    link_rows = [l[1:] for l in links]
    write_bulk_insert(f, "Links", ["title", "description", "url", "isPrivate", "ownerId", "createdAt"], link_rows, unicode_mask=[True, True, False, False, False, False])

    # LinkCategories
    f.write("-- 4. CATEGORÍAS DE LOS ENLACES\n")
    write_bulk_insert(f, "LinkCategories", ["linkId", "categoryId"], link_categories)

    # Friendships
    f.write("-- 5. AMISTADES\n")
    write_bulk_insert(f, "Friendships", ["requesterId", "addresseeId", "status", "createdAt"], friendships)

    # SharedLinks
    f.write("-- 6. ENLACES COMPARTIDOS\n")
    write_bulk_insert(f, "SharedLinks", ["linkId", "userId"], shared_links)

print(f"¡Listo! Se ha generado el archivo {filename}")
