# 🎨 Portfolio API

API REST multitenant para la gestión y visualización de portafolios vocacionales en el área de Tecnologías de la Información y Comunicación (TIC).

## 📋 Descripción

Portfolio API es una plataforma backend diseñada para permitir a personas del área TIC compartir sus proyectos, habilidades y experiencias de manera vocacional (no profesional). Utiliza una arquitectura multitenant que permite aislar los datos de cada usuario mientras comparten la misma infraestructura.

## 🚀 Características

- ✅ **Multitenant**: Arquitectura que permite múltiples usuarios con datos aislados
- 🔐 **Autenticación y autorización**: Control de acceso seguro
- 🗄️ **Base de datos PostgreSQL**: Almacenamiento robusto y escalable
- 📦 **Entity Framework Core**: ORM moderno para .NET
- 🐳 **Docker**: Contenerización para despliegue fácil
- 📖 **OpenAPI/Swagger**: Documentación interactiva de la API
- 🔄 **RESTful**: Diseño de API siguiendo estándares REST

## 🛠️ Tecnologías

- **Framework**: ASP.NET Core 10.0
- **Lenguaje**: C# con Nullable habilitado
- **Base de datos**: PostgreSQL
- **ORM**: Entity Framework Core 10.0.3
- **Documentación**: OpenAPI 10.0.3
- **Contenedores**: Docker (Linux)

## 📦 Dependencias principales

```xml
- Microsoft.EntityFrameworkCore (10.0.3)
- Npgsql.EntityFrameworkCore.PostgreSQL (10.0.0)
- Microsoft.AspNetCore.OpenApi (10.0.3)
- Microsoft.EntityFrameworkCore.Tools (10.0.3)
- Microsoft.EntityFrameworkCore.Design (10.0.3)
```

## 🏗️ Estructura del proyecto

```
portfolio_api/
├── Controllers/         # Controladores de la API
├── Data/               # Contexto de base de datos y configuraciones
├── Models/             # Modelos de dominio
├── Properties/         # Configuraciones del proyecto
├── Dockerfile          # Configuración de Docker
├── Program.cs          # Punto de entrada de la aplicación
├── appsettings.json    # Configuración de la aplicación
└── portfolio_api.csproj # Archivo del proyecto .NET
```

## 🔧 Requisitos previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (o Docker)
- [Docker](https://www.docker.com/get-started) (opcional, para contenedores)

## 📥 Instalación

### Opción 1: Ejecución local

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/016jesus/portfolio_api.git
   cd portfolio_api
   ```

2. **Configurar la cadena de conexión**
   
   Edita `appsettings.json` y configura tu cadena de conexión a PostgreSQL:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=portfolio_db;Username=tu_usuario;Password=tu_password"
     }
   }
   ```

3. **Aplicar migraciones**
   ```bash
   dotnet ef database update
   ```

4. **Ejecutar la aplicación**
   ```bash
   dotnet run
   ```

5. **Acceder a la API**
   - API: `https://localhost:5001`
   - Swagger UI (en desarrollo): `https://localhost:5001/openapi`

### Opción 2: Ejecución con Docker

1. **Construir la imagen**
   ```bash
   docker build -t portfolio-api .
   ```

2. **Ejecutar el contenedor**
   ```bash
   docker run -p 8080:8080 -p 8081:8081 \
     -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=portfolio_db;Username=tu_usuario;Password=tu_password" \
     portfolio-api
   ```

### Opción 3: Docker Compose (recomendado)

Crea un archivo `docker-compose.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: portfolio_db
      POSTGRES_USER: portfolio_user
      POSTGRES_PASSWORD: portfolio_pass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=portfolio_db;Username=portfolio_user;Password=portfolio_pass"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

Luego ejecuta:
```bash
docker-compose up -d
```

## 🔐 Configuración

### Variables de entorno

```bash
# Cadena de conexión a PostgreSQL
ConnectionStrings__DefaultConnection="Host=localhost;Database=portfolio_db;Username=user;Password=pass"

# Ambiente
ASPNETCORE_ENVIRONMENT=Development
```

### User Secrets (para desarrollo)

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "tu_cadena_de_conexion"
```

## 📚 Uso de la API

### Ejemplos de endpoints (a implementar)

```bash
# Obtener todos los portafolios
GET /api/portfolios

# Obtener un portafolio específico
GET /api/portfolios/{id}

# Crear un nuevo portafolio
POST /api/portfolios
Content-Type: application/json
{
  "nombre": "Mi Portfolio",
  "descripcion": "Portfolio de desarrollo web",
  "tecnologias": ["C#", "React", "PostgreSQL"]
}

# Actualizar un portafolio
PUT /api/portfolios/{id}

# Eliminar un portafolio
DELETE /api/portfolios/{id}
```

## 🧪 Pruebas

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con cobertura
dotnet test /p:CollectCoverage=true
```

## 📊 Migraciones de base de datos

```bash
# Crear una nueva migración
dotnet ef migrations add NombreDeLaMigracion

# Aplicar migraciones
dotnet ef database update

# Revertir a una migración específica
dotnet ef database update NombreDeLaMigracion

# Eliminar última migración
dotnet ef migrations remove
```

## 🤝 Contribución

¡Las contribuciones son bienvenidas! Por favor, sigue estos pasos:

1. Haz fork del proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add: nueva funcionalidad increíble'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

### Guía de estilo

- Usa convenciones de nomenclatura de C# (.NET)
- Documenta tu código con comentarios XML
- Escribe pruebas unitarias para nuevas funcionalidades
- Sigue los principios SOLID

## 📝 Roadmap

- [ ] Implementar modelos de dominio (Usuario, Portfolio, Proyecto, Habilidad)
- [ ] Sistema de autenticación JWT
- [ ] Implementar arquitectura multitenant completa
- [ ] Agregar endpoints CRUD para portfolios
- [ ] Sistema de categorías y etiquetas
- [ ] Carga y gestión de imágenes
- [ ] Filtros y búsqueda avanzada
- [ ] Paginación de resultados
- [ ] Validación de modelos con FluentValidation
- [ ] Logging con Serilog
- [ ] Caché con Redis
- [ ] Rate limiting
- [ ] Pruebas unitarias e integración
- [ ] CI/CD con GitHub Actions
- [ ] Documentación completa de API

## 📄 Licencia

Este proyecto es de código abierto y está disponible bajo una licencia permisiva.

## 👤 Autor

**016jesus**
- GitHub: [@016jesus](https://github.com/016jesus)

## 🙏 Agradecimientos

- Comunidad .NET
- Contributors de Entity Framework Core
- Desarrolladores de PostgreSQL
- Equipo de ASP.NET Core

---

⭐️ Si este proyecto te resulta útil, ¡considera darle una estrella!

## 📞 Soporte

Si tienes preguntas o necesitas ayuda, puedes:
- Abrir un [issue](https://github.com/016jesus/portfolio_api/issues)
- Consultar la documentación de OpenAPI en `/openapi` (en modo desarrollo)

---

**Nota**: Este es un proyecto vocacional para el área TIC, diseñado para aprender y compartir conocimientos. ¡Todas las contribuciones y sugerencias son bienvenidas! 🚀
