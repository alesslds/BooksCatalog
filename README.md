# BooksCatalog API

API REST para gestionar un catálogo de libros, desarrollada en **.NET 8** con **PostgreSQL** y preparada para despliegue en **AWS Lambda** (API Gateway + RDS + Secrets Manager).

Esta solución responde a los requisitos de la prueba técnica:

- ✅ CRUD completo sobre una tabla `books`
- ✅ .NET 8 (compatible conceptualmente con .NET 6/7)
- ✅ Base de datos PostgreSQL con stored procedures
- ✅ Clean Architecture con separación por capas
- ✅ Manejo de errores con middleware global (Problem Details)
- ✅ Validaciones de entrada centralizadas
- ✅ Concurrencia optimista en actualizaciones
- ✅ Configuración para despliegue en AWS Lambda
- ✅ Postman Collection con pruebas automatizadas
- ✅ Script SQL completo para base de datos

---

## 📋 Tabla de Contenidos

- [Arquitectura](#arquitectura)
- [Tecnologías Utilizadas](#tecnologías-utilizadas)
- [Requisitos Previos](#requisitos-previos)
- [Configuración de Base de Datos](#configuración-de-base-de-datos)
- [Ejecución Local](#ejecución-local)
- [Endpoints de la API](#endpoints-de-la-api)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Despliegue en AWS Lambda](#despliegue-en-aws-lambda)
- [Pruebas con Postman](#pruebas-con-postman)
- [Buenas Prácticas Implementadas](#buenas-prácticas-implementadas)

---

## 🏗️ Arquitectura

El proyecto sigue los principios de **Clean Architecture** con separación clara de responsabilidades en 4 capas:

```
BooksCatalog.Domain          → Entidades y excepciones de dominio
BooksCatalog.Application     → DTOs, servicios, validaciones y contratos
BooksCatalog.Infrastructure  → Implementación de repositorios y acceso a datos
BooksCatalog.Api             → Controllers, middleware y configuración de API
```

### Capas del Proyecto

#### 📦 **BooksCatalog.Domain**
- **Entidades de dominio**: `Book` con propiedades inmutables
- **Excepciones personalizadas**:
  - `NotFoundException`: Cuando un recurso no existe (404)
  - `DomainValidationException`: Errores de validación de negocio (400)
  - `ConcurrencyException`: Conflictos de concurrencia optimista (409)
  - `DomainException`: Excepción base para errores de dominio

#### 📦 **BooksCatalog.Application**
- **DTOs**:
  - `BookDto`: Representación de salida de un libro
  - `CreateBookRequest`: Payload para crear libros
  - `UpdateBookRequest`: Payload para actualizar libros (incluye version para concurrencia)
- **Servicios**: `IBookService` / `BookService` con lógica de negocio
- **Validaciones**: `BookValidator` con reglas de negocio:
  - Título: máximo 200 caracteres
  - Autor: máximo 100 caracteres
  - Año de publicación: entre 1000 y año actual
  - Páginas: entre 1 y 10,000
  - ISBN: formato válido (10 o 13 dígitos)
  - Categoría: máximo 50 caracteres
- **Modelos auxiliares**: `PagedResult<T>` para paginación

#### 📦 **BooksCatalog.Infrastructure**
- **Repositorio**: `BookRepository` implementa `IBookRepository`
- **Tecnología**: Dapper + Npgsql para acceso a datos
- **Stored Procedures**: Todo el acceso a datos se realiza mediante:
  - `usp_books_create`: Insertar nuevo libro
  - `usp_books_get_by_id`: Obtener libro por ID
  - `usp_books_list_paged`: Listar libros con paginación y filtros
  - `usp_books_update`: Actualizar libro (con control de concurrencia)
  - `usp_books_soft_delete`: Eliminación lógica
- **Configuración**: `IDbConnectionStringProvider` con dos implementaciones:
  - Local: Lee `ConnectionStrings:BooksDb` de `appsettings.Development.json`
  - AWS Lambda: Lee connection string desde **AWS Secrets Manager**

#### 📦 **BooksCatalog.Api**
- **Controllers**: `BooksController` expone endpoints REST en `/api/Books`
- **Middleware**: `ErrorHandlingMiddleware` convierte excepciones en respuestas Problem Details:
  - 400 Bad Request: Errores de validación
  - 404 Not Found: Recurso no encontrado
  - 409 Conflict: Conflicto de concurrencia
  - 500 Internal Server Error: Errores inesperados
- **Hosting flexible**:
  - `LocalEntryPoint`: Ejecución local con Kestrel
  - `LambdaEntryPoint`: Ejecución en AWS Lambda con Amazon.Lambda.AspNetCoreServer
- **Swagger**: Documentación interactiva habilitada en entorno `Development`

---

## 🛠️ Tecnologías Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core** - Web API
- **PostgreSQL** - Base de datos relacional
- **Npgsql** - Proveedor de datos .NET para PostgreSQL
- **Dapper** - Micro-ORM para acceso a datos
- **Swashbuckle (Swagger)** - Documentación de API
- **Amazon.Lambda.AspNetCoreServer** - Hosting en AWS Lambda
- **AWS Secrets Manager** - Gestión segura de credenciales
- **FluentValidation** (implícita) - Validaciones

### Dependencias Principales

```xml
<PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="9.2.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="10.0.1" />
<PackageReference Include="Npgsql" Version="8.0.x" />
<PackageReference Include="Dapper" Version="2.1.x" />
<PackageReference Include="AWSSDK.SecretsManager" Version="3.7.x" />
```

---

## 📋 Requisitos Previos

Antes de ejecutar el proyecto, asegúrate de tener instalado:

1. **.NET 8 SDK** - [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **PostgreSQL 12+** - [Descargar aquí](https://www.postgresql.org/download/)
3. **Git** - Para clonar el repositorio
4. **Visual Studio 2022** o **VS Code** (opcional pero recomendado)
5. **Postman** - Para probar los endpoints

### Verificar Instalación de .NET

```bash
dotnet --version
# Debe mostrar: 8.0.x o superior
```

### Verificar Instalación de PostgreSQL

```bash
psql --version
# Debe mostrar: psql (PostgreSQL) 12.x o superior
```

---

## 🗄️ Configuración de Base de Datos

### Paso 1: Crear la Base de Datos

Conectarse a PostgreSQL y ejecutar:

```sql
CREATE DATABASE books_catalog
    WITH 
        OWNER = postgres
        ENCODING = 'UTF8'
        LC_COLLATE = 'en_US.utf8'
        LC_CTYPE = 'en_US.utf8'
        TEMPLATE = template0;
```

**Nota**: Si estás en Windows, puedes usar:

```sql
CREATE DATABASE books_catalog
    WITH 
        OWNER = postgres
        ENCODING = 'UTF8';
```

### Paso 2: Ejecutar el Script SQL

El script `database_script.sql` incluye:

1. **Tabla `books`** con las siguientes columnas:
   - `id` (UUID, PK)
   - `title` (VARCHAR(200), NOT NULL)
   - `author` (VARCHAR(100), NOT NULL)
   - `publication_year` (INT, NOT NULL)
   - `publisher` (VARCHAR(100), NULL)
   - `page_count` (INT, NOT NULL)
   - `category` (VARCHAR(50), NULL)
   - `isbn` (VARCHAR(20), NULL)
   - `language` (VARCHAR(10), NULL)
   - `is_deleted` (BOOLEAN, DEFAULT FALSE) - Soft delete
   - `created_at` (TIMESTAMP, DEFAULT NOW())
   - `updated_at` (TIMESTAMP, DEFAULT NOW())
   - `version` (INT, DEFAULT 1) - Control de concurrencia optimista

2. **Índices** para optimización:
   - Índice en `is_deleted` (filtros de eliminación lógica)
   - Índice en `category` (búsquedas por categoría)
   - Índice compuesto en `title` y `author` (búsquedas de texto)

3. **Stored Procedures**:
   - `usp_books_create`
   - `usp_books_get_by_id`
   - `usp_books_list_paged`
   - `usp_books_update`
   - `usp_books_soft_delete`

4. **Datos de prueba** (opcional)

#### Ejecutar desde línea de comandos:

```bash
psql -U postgres -d books_catalog -f database_script.sql
```

#### Ejecutar desde pgAdmin:

1. Conectar a la base de datos `books_catalog`
2. Abrir el Query Tool
3. Cargar y ejecutar el archivo `database_script.sql`

### Paso 3: Configurar Connection String

Editar el archivo `BooksCatalog.Api/src/BooksCatalog.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "BooksDb": "Host=upch-dev.cm5ssigm2s64.us-east-1.rds.amazonaws.com;Port=5432;Database=books_catalog;Username=postgres;Password=postgresupch;Ssl Mode=Require;Trust Server Certificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Importante**: Reemplaza `TU_PASSWORD` con tu contraseña real de PostgreSQL.

---

## 🚀 Ejecución Local

### Opción 1: Desde Visual Studio 2022

1. Abrir el archivo `BooksCatalog.sln`
2. Establecer `BooksCatalog.Api` como proyecto de inicio
3. Presionar `F5` o hacer clic en el botón "Run"
4. La API se iniciará en `https://localhost:61824` (o el puerto configurado)
5. Navegar a `https://localhost:61824/swagger` para ver la documentación interactiva

### Opción 2: Desde línea de comandos

```bash
# 1. Clonar el repositorio (reemplazar con tu URL)
git clone https://github.com/tu-usuario/books-catalog-api.git
cd books-catalog-api

# 2. Restaurar dependencias
dotnet restore

# 3. Navegar a la carpeta del proyecto API
cd BooksCatalog.Api/src/BooksCatalog.Api

# 4. Ejecutar el proyecto
dotnet run
```

La API estará disponible en:
- HTTP: `http://localhost:61825`
- HTTPS: `https://localhost:61824`
- Swagger: `https://localhost:5001/swagger`

### Opción 3: Desde VS Code

```bash
# 1. Abrir el proyecto en VS Code
code .

# 2. Presionar F5 para ejecutar con debugger
# O usar la terminal integrada:
cd BooksCatalog.Api/src/BooksCatalog.Api
dotnet run
```

### Verificar que la API está funcionando

```bash
# Hacer una petición de prueba
curl https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books

# O abrir en el navegador:
# https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books
```

---

## 📡 Endpoints de la API

Todos los endpoints están bajo la ruta base: `/api/Books`

### 1. Listar Libros (Paginado con Filtros)

```http
GET /api/books?search={texto}&category={categoria}&pageNumber={numero}&pageSize={tamaño}
```

**Query Parameters** (todos opcionales):
- `search` (string): Busca en título y autor
- `category` (string): Filtra por categoría exacta
- `pageNumber` (int, default=1): Número de página
- `pageSize` (int, default=10): Tamaño de página (máx. 100)

**Respuesta Exitosa** (200 OK):
```json
{
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "title": "Clean Architecture",
      "author": "Robert C. Martin",
      "publicationYear": 2017,
      "publisher": "Pearson",
      "pageCount": 432,
      "category": "Software Engineering",
      "isbn": "9780134494166",
      "language": "en",
      "createdAt": "2024-11-01T10:30:00Z",
      "updatedAt": "2024-11-01T10:30:00Z",
      "version": 1
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalItems": 42,
  "totalPages": 5
}
```

**Ejemplo de uso**:
```bash
# Listar todos los libros (primera página)
curl http://localhost:5000/api/books

# Buscar libros con "clean" en título o autor
curl "http://localhost:5000/api/books?search=clean"

# Filtrar por categoría
curl "http://localhost:5000/api/books?category=Software%20Engineering"

# Combinar búsqueda y paginación
curl "http://localhost:5000/api/books?search=architecture&pageNumber=2&pageSize=5"
```

---

### 2. Obtener Libro por ID

```http
GET /api/books/{id}
```

**Path Parameters**:
- `id` (UUID): ID del libro

**Respuesta Exitosa** (200 OK):
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "title": "Clean Architecture",
  "author": "Robert C. Martin",
  "publicationYear": 2017,
  "publisher": "Pearson",
  "pageCount": 432,
  "category": "Software Engineering",
  "isbn": "9780134494166",
  "language": "en",
  "createdAt": "2024-11-01T10:30:00Z",
  "updatedAt": "2024-11-01T10:30:00Z",
  "version": 1
}
```

**Respuesta de Error** (404 Not Found):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Libro con ID '123e4567-e89b-12d3-a456-426614174000' no encontrado."
}
```

**Ejemplo de uso**:
```bash
curl https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books/123e4567-e89b-12d3-a456-426614174000
```

---

### 3. Crear Libro

```http
POST /api/books
Content-Type: application/json
```

**Request Body**:
```json
{
  "title": "Domain-Driven Design",
  "author": "Eric Evans",
  "publicationYear": 2003,
  "publisher": "Addison-Wesley",
  "pageCount": 560,
  "category": "Software Engineering",
  "isbn": "9780321125217",
  "language": "en"
}
```

**Campos requeridos**:
- `title` (string, max 200): Título del libro
- `author` (string, max 100): Nombre del autor
- `publicationYear` (int, 1000-año actual): Año de publicación
- `pageCount` (int, 1-10000): Número de páginas

**Campos opcionales**:
- `publisher` (string, max 100): Editorial
- `category` (string, max 50): Categoría
- `isbn` (string, max 20): ISBN (validado)
- `language` (string, max 10): Código de idioma (ej: "es", "en")

**Respuesta Exitosa** (201 Created):
```json
{
  "id": "456e7890-e89b-12d3-a456-426614174001",
  "title": "Domain-Driven Design",
  "author": "Eric Evans",
  "publicationYear": 2003,
  "publisher": "Addison-Wesley",
  "pageCount": 560,
  "category": "Software Engineering",
  "isbn": "9780321125217",
  "language": "en",
  "createdAt": "2024-11-04T15:45:00Z",
  "updatedAt": "2024-11-04T15:45:00Z",
  "version": 1
}
```

**Headers de respuesta**:
```
Location: /api/books/456e7890-e89b-12d3-a456-426614174001
```

**Respuesta de Error** (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "El título no puede estar vacío.",
  "errors": {
    "Title": ["El título no puede estar vacío."],
    "PageCount": ["El número de páginas debe ser mayor a 0."]
  }
}
```

**Ejemplo de uso**:
```bash
curl -X POST https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Domain-Driven Design",
    "author": "Eric Evans",
    "publicationYear": 2003,
    "publisher": "Addison-Wesley",
    "pageCount": 560,
    "category": "Software Engineering",
    "isbn": "9780321125217",
    "language": "en"
  }'
```

---

### 4. Actualizar Libro

```http
PUT /api/books/{id}
Content-Type: application/json
```

**Path Parameters**:
- `id` (UUID): ID del libro a actualizar

**Request Body**:
```json
{
  "title": "Clean Architecture (Updated)",
  "author": "Robert C. Martin (Uncle Bob)",
  "publicationYear": 2017,
  "publisher": "Pearson Education",
  "pageCount": 432,
  "category": "Software Architecture",
  "isbn": "9780134494166",
  "language": "en",
  "version": 1
}
```

**Importante**: 
- Se deben enviar **todos los campos** (incluso los que no cambian)
- El campo `version` es **obligatorio** para control de concurrencia optimista

**Respuesta Exitosa** (204 No Content):
- Sin body
- Indica que la actualización fue exitosa

**Respuestas de Error**:

**404 Not Found** - Libro no existe:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Libro con ID '...' no encontrado."
}
```

**409 Conflict** - Conflicto de concurrencia:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Concurrency Conflict",
  "status": 409,
  "detail": "El libro fue modificado por otro usuario. Por favor, recarga y vuelve a intentar."
}
```

**400 Bad Request** - Error de validación:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "El año de publicación no puede ser futuro.",
  "errors": {
    "PublicationYear": ["El año de publicación no puede ser mayor a 2024."]
  }
}
```

**Ejemplo de uso**:
```bash
curl -X PUT https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books/123e4567-e89b-12d3-a456-426614174000 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Clean Architecture (Updated)",
    "author": "Robert C. Martin",
    "publicationYear": 2017,
    "publisher": "Pearson Education",
    "pageCount": 432,
    "category": "Software Architecture",
    "isbn": "9780134494166",
    "language": "en",
    "version": 1
  }'
```

---

### 5. Eliminar Libro (Soft Delete)

```http
DELETE /api/books/{id}
```

**Path Parameters**:
- `id` (UUID): ID del libro a eliminar

**Respuesta Exitosa** (204 No Content):
- Sin body
- El libro se marca como eliminado (`is_deleted = true`)
- No se elimina físicamente de la base de datos

**Respuesta de Error** (404 Not Found):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Libro con ID '...' no encontrado o ya fue eliminado."
}
```

**Ejemplo de uso**:
```bash
curl -X DELETE https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books/123e4567-e89b-12d3-a456-426614174000
```

**Nota**: Los libros eliminados no aparecen en las consultas de listado o búsqueda por ID.

---

## 📁 Estructura del Proyecto

```
BooksCatalog/
├── BooksCatalog.sln                          # Archivo de solución
├── README.md                                 # Este archivo
├── database_script.sql                       # Script SQL completo
├── .gitignore                                # Archivos excluidos de Git
│
├── BooksCatalog.Domain/                      # Capa de Dominio
│   ├── Entities/
│   │   └── Book.cs                           # Entidad principal
│   ├── Exceptions/
│   │   ├── DomainException.cs                # Excepción base
│   │   ├── NotFoundException.cs              # 404
│   │   ├── DomainValidationException.cs      # 400
│   │   └── ConcurrencyException.cs           # 409
│   └── BooksCatalog.Domain.csproj
│
├── BooksCatalog.Application/                 # Capa de Aplicación
│   ├── DTOs/
│   │   ├── BookDto.cs                        # DTO de salida
│   │   ├── CreateBookRequest.cs              # DTO de creación
│   │   └── UpdateBookRequest.cs              # DTO de actualización
│   ├── Services/
│   │   ├── IBookService.cs                   # Contrato de servicio
│   │   └── BookService.cs                    # Implementación
│   ├── Repositories/
│   │   └── IBookRepository.cs                # Contrato de repositorio
│   ├── Validation/
│   │   └── BookValidator.cs                  # Validaciones de negocio
│   ├── Common/
│   │   └── Models/
│   │       └── PagedResult.cs                # Modelo de paginación
│   ├── DependencyInjection.cs                # Registro de servicios
│   └── BooksCatalog.Application.csproj
│
├── BooksCatalog.Infrastructure/              # Capa de Infraestructura
│   ├── Data/
│   │   └── BookRepository.cs                 # Implementación con Dapper
│   ├── Configuration/
│   │   ├── IDbConnectionStringProvider.cs    # Contrato para connection string
│   │   └── SecretsManagerConnectionStringProvider.cs  # AWS Secrets Manager
│   ├── DependencyInjection.cs                # Registro de servicios
│   └── BooksCatalog.Infrastructure.csproj
│
└── BooksCatalog.Api/                         # Capa de Presentación
    ├── src/
    │   └── BooksCatalog.Api/
    │       ├── Controllers/
    │       │   └── BooksController.cs        # Endpoints REST
    │       ├── Middleware/
    │       │   └── ErrorHandlingMiddleware.cs # Manejo global de errores
    │       ├── Properties/
    │       │   └── launchSettings.json       # Configuración de ejecución
    │       ├── appsettings.json              # Configuración general
    │       ├── appsettings.Development.json  # Configuración de desarrollo
    │       ├── aws-lambda-tools-defaults.json # Configuración Lambda
    │       ├── serverless.template           # AWS SAM template
    │       ├── Startup.cs                    # Configuración de servicios
    │       ├── LocalEntryPoint.cs            # Entry point local
    │       ├── LambdaEntryPoint.cs           # Entry point AWS Lambda
    │       └── BooksCatalog.Api.csproj
    │
    └── test/
        └── BooksCatalog.Api.Tests/           # Proyecto de pruebas
            ├── ValuesControllerTests.cs
            ├── appsettings.json
            └── BooksCatalog.Api.Tests.csproj

Postman/
├── books.collection.json                     # Colección de Postman (Lambda)
└── books.local.collection.json               # Colección de Postman (Local)
```

---

## ☁️ Despliegue en AWS Lambda

El proyecto incluye configuración completa para despliegue en AWS Lambda.

### Requisitos AWS

1. **Cuenta de AWS** activa
2. **AWS CLI** configurado
3. **AWS Toolkit for Visual Studio** (opcional)
4. **Amazon.Lambda.Tools** instalado:
   ```bash
   dotnet tool install -g Amazon.Lambda.Tools
   ```

### Recursos AWS Necesarios

1. **RDS PostgreSQL** (o Aurora PostgreSQL)
2. **AWS Secrets Manager** con secret que contenga:
   ```json
   {
     "username": "postgres",
     "password": "tu-password-seguro",
     "engine": "postgres",
     "host": "tu-instancia-rds.region.rds.amazonaws.com",
     "port": 5432,
     "dbname": "books_catalog"
   }
   ```
3. **API Gateway** (creado automáticamente por SAM)
4. **IAM Role** con permisos para:
   - Lambda execution
   - Secrets Manager (GetSecretValue)
   - RDS network access

### Pasos para Despliegue

#### 1. Configurar Variables de Entorno

Editar `aws-lambda-tools-defaults.json`:

```json
{
  "profile": "default",
  "region": "us-east-1",
  "configuration": "Release",
  "framework": "net8.0",
  "s3-bucket": "tu-bucket-para-deployment",
  "stack-name": "bookscatalog-api",
  "s3-prefix": "bookscatalog/"
}
```

#### 2. Configurar Secrets Manager

En `serverless.template`, asegurar que la variable de entorno esté configurada:

```json
{
  "Environment": {
    "Variables": {
      "BooksDb__SecretName": "books-catalog/db-connection",
      "ASPNETCORE_ENVIRONMENT": "Production"
    }
  }
}
```

#### 3. Desplegar desde línea de comandos

```bash
# Desde la carpeta BooksCatalog.Api/src/BooksCatalog.Api/
dotnet lambda deploy-serverless
```

#### 4. Desplegar desde Visual Studio

1. Click derecho en el proyecto `BooksCatalog.Api`
2. Seleccionar "Publish to AWS Lambda"
3. Seguir el asistente de despliegue

### Configuración del Security Group

El Lambda debe tener acceso a la instancia RDS:

1. Crear un Security Group para Lambda
2. Agregar regla OUTBOUND al Security Group de RDS:
   - Type: PostgreSQL
   - Port: 5432
   - Source: Security Group del Lambda

### Verificar Despliegue

```bash
# Obtener la URL del API Gateway
aws cloudformation describe-stacks \
  --stack-name bookscatalog-api \
  --query 'Stacks[0].Outputs[?OutputKey==`ApiURL`].OutputValue' \
  --output text

# Probar el endpoint
curl https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/api/books
```

---

## 📮 Pruebas con Postman

El proyecto incluye dos colecciones de Postman:

1. **books.local.collection.json** - Para pruebas locales
2. **books.collection.json** - Para pruebas en AWS Lambda

### Importar Colección en Postman

1. Abrir Postman
2. Click en "Import"
3. Seleccionar el archivo `books.local.collection.json`
4. La colección se importará con todas las pruebas automatizadas

### Configurar Variables de Entorno

Crear un Environment en Postman con la variable:

```
baseUrl = http://localhost:61824
```

O para AWS Lambda:

```
baseUrl = https://9knw8u1ff8.execute-api.us-east-1.amazonaws.com/Prod/
```

### Pruebas Incluidas

Cada request incluye **tests automatizados** que validan:

✅ **GET /api/books (List)**
- Status code 200
- Estructura de paginación correcta
- Propiedades items, pageNumber, pageSize, totalItems

✅ **POST /api/books (Create)**
- Status code 201
- Header Location presente
- Response contiene ID generado
- Todos los campos retornados correctamente

✅ **GET /api/books/{id} (Get by ID)**
- Status code 200
- Estructura del libro correcta
- ID coincide con el solicitado

✅ **PUT /api/books/{id} (Update)**
- Status code 204 (No Content)
- Validación de conflicto de concurrencia

✅ **DELETE /api/books/{id} (Delete)**
- Status code 204 (No Content)
- Libro ya no aparece en listados

### Ejecutar Todas las Pruebas

1. Seleccionar la colección en Postman
2. Click en "Run collection"
3. Se ejecutarán todos los tests automáticamente
4. Ver reporte de resultados

---

## ✨ Buenas Prácticas Implementadas

### 1. Arquitectura Limpia (Clean Architecture)

✅ **Separación de capas** con dependencias hacia el interior
✅ **Domain-Centric Design** - La capa de dominio no tiene dependencias externas
✅ **Inversión de dependencias** mediante interfaces
✅ **SOLID Principles**

### 2. Manejo de Errores

✅ **Middleware global** para captura de excepciones
✅ **Problem Details** (RFC 7807) para respuestas de error consistentes
✅ **Códigos de estado HTTP apropiados**:
- 200 OK - Éxito
- 201 Created - Recurso creado
- 204 No Content - Éxito sin contenido
- 400 Bad Request - Error de validación
- 404 Not Found - Recurso no encontrado
- 409 Conflict - Conflicto de concurrencia
- 500 Internal Server Error - Error del servidor

### 3. Validaciones

✅ **Validaciones de entrada** en la capa de aplicación
✅ **Validaciones de negocio** centralizadas en `BookValidator`
✅ **Mensajes de error descriptivos** para el cliente

### 4. Concurrencia

✅ **Concurrencia optimista** usando campo `version`
✅ **Prevención de actualizaciones conflictivas**
✅ **Mensajes claros** cuando hay conflictos

### 5. Base de Datos

✅ **Stored Procedures** para todo el acceso a datos
✅ **Índices** para optimización de consultas
✅ **Soft Delete** - No se eliminan registros físicamente
✅ **Auditoría** con campos `created_at` y `updated_at`
✅ **UUIDs** en lugar de IDs secuenciales (seguridad)

### 6. API Design

✅ **RESTful** con verbos HTTP semánticos
✅ **Paginación** en listados
✅ **Filtros opcionales** (search, category)
✅ **Swagger/OpenAPI** para documentación
✅ **Versionado implícito** en la ruta (`/api/[controller]`)


---

## 🔧 Comandos Útiles

### .NET CLI

```bash
# Restaurar dependencias
dotnet restore

# Compilar proyecto
dotnet build

# Ejecutar proyecto
dotnet run --project BooksCatalog.Api/src/BooksCatalog.Api

# Ejecutar tests
dotnet test

# Limpiar build artifacts
dotnet clean

# Publicar para producción
dotnet publish -c Release -o ./publish
```

### Git

```bash
# Clonar repositorio
git clone https://github.com/alesslds/BooksCatalog.git

# Ver cambios
git status

# Agregar cambios
git add .

# Commit
git commit -m "Descripción del cambio"

# Push
git push origin main
```
