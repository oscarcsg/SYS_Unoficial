# USERS

- [x] **GET /api/users** &rarr; Trae todos los usuarios. Si se añade un parámetro mediante _?_ se pueden buscar los usuarios por alias o email.

- [x] **GET /api/users/search?search=** &rarr; Trae una lista ultraligera de usuarios filtrando por alias o email (para la barra de búsqueda de amigos).

- [x] **GET /api/users/{userId}** &rarr; Trae el perfil público (los datos) del usuario con ese id.

- [x] **POST /api/users** &rarr; Crea un nuevo usuario en la base de datos.

- [x] **POST /api/users/login** &rarr; Inicia sesión con los datos que se le pasen mediante el formulario de inicio de sesión y se devuleve el JWT.

- [x] **PUT /api/users/profile** &rarr; Actualiza datos del perfil del usuario actual, usando el token para obtener su id.

## USERS/LINKS

- [x] **GET /api/users/{userId}/links** &rarr; Trae los links públicos del usuario que tiene ese id.

- [x] **GET /api/users/{userId}/categories** &rarr; Trae las categorías públicas del usuario que se esté viendo (el del id).

# LINKS

- [x] **GET /api/links** &rarr; Usa el token para traer los links de ese usuario. Sólo trae datos para mostrar en un listado, no los detalles.

- [x] **GET /api/links/{linkId}** &rarr; Trae el detalle de un link específico (el de la id).

- [x] **POST /api/links** &rarr; Crea un link, el ownerId se saca del token.

- [x] **PUT /api/links/{linkId}** &rarr; Editar un link existente.

- [x] **DELETE /api/links/{linkId}** &rarr; Borra el link indicado, pero sólo si el ownerId coincide con el id el token (se controla en código).

## LINKS COMPARTIDOS

- [x] **GET /api/links/shared-with-me** &rarr; Trae los links compartidos con el usuario logeado.

- [x] **POST /api/links/{linkId}/share-with/{friendId}** &rarr; Recibe el id de un amigo y crea el registro en la tabla SharedLinks. Comparte el link con el usuario seleccionado.

- [x] **DELETE /api/links/{linkId}/share/{friendId}** &rarr; Revoca el acceso a un enlace compartido al amigo indicado.

# AMISTADES

- [x] **GET /api/friendships/pending** &rarr; Trae las solicitudes que se han enviado al usuario logeado.

- [x] **GET /api/friendships** &rarr; Trae la lista de amigos aceptado (status = 1).

- [x] **POST /api/friendships/request/{addreseeId}** &rarr; El usuario logeado le envía solicitud de amistad al id de la url.

- [x] **PUT /api/friendships/respond/{requesterId}** &rarr; Modifica la solicitud con otros usuarios usando el token, el id del usuario cuya relacion se quiere modificar y un json con el nuevo estado.

- [x] **DELETE /api/friendships/remove/{friendId}** &rarr; Elimina a un amigo existente de la lista de amigos.

# CATEGORÍAS

- [x] **GET /api/categories** &rarr; Trae las categorías del sistema **Y** las del usuario logeado (mediante el token).

- [x] **POST /api/categories** &rarr; Crea una nueva categoría, con el ownerId del token del usuario logeado.

- [x] **PUT /api/categories/{categoryId}** &rarr; Edita datos de una categoría del usuario. Siempre y cuando el ownerId coincida con el id del token.

- [x] **DELETE /api/categories/{categoryId}** &rarr; Elimina una categoría siempre y cuando el ownerId coincida con el id del token. También debe eliminarse de la cascada intermedia LinkCategories.
