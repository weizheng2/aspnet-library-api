# ASP.NET Library API
A RESTful ASP.NET Core Web API demonstration for a **library management platform**, allowing users to manage authors, books, and comments with authentication and role-based authorization.  

## 🛠️ Technologies Used
- ASP.NET Core
- Entity Framework Core with SQL database
- Unit and Integration Tests with xUnit
- Controller → Service pattern
- JWT Authentication
- Output Cache with Redis
- API Versioning
- Rate Limiting
- Swagger/OpenAPI
- Azure Blob Storage for file uploads

## 🌐 Live Demo
Access the live API via Swagger (may take a few seconds to start if idle):  
[https://library-api.runasp.net/swagger/index.html](https://library-api.runasp.net/swagger/index.html)

---

## API Endpoints

<details>
<summary>✍️ Authors Endpoints</summary>

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/authors` | Get all authors | ❌ |
| `GET` | `/authors/with-filter` | Get authors by filter criteria | ❌ |
| `GET` | `/authors/{id}` | Get author by ID | ❌ |
| `POST` | `/authors` | Add author (with or without books) | ✅ |
| `POST` | `/authors/with-photo` | Add author with photo (with or without books) | ✅ |
| `PUT` | `/authors/{id}` | Update author | ✅ |
| `DELETE` | `/authors/{id}` | Delete author | ✅ |

</details>

<details>
<summary>🗂️ Authors Collection Endpoints</summary>
  
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/authors-collection/{ids}` | Get a collection of authors (with or without books) | ✅ |
| `POST` | `/authors-collection` | Create a collection of authors (with or without books) | ✅ |

</details>

<details>
<summary>📖 Books Endpoints</summary>
  
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/books` | Get all books | ❌ |
| `GET` | `/books/{id}` | Get book by ID | ❌ |
| `POST` | `/books` | Create new book | ✅ |
| `PUT` | `/books/{id}` | Update book | ✅ |
| `DELETE` | `/books/{id}` | Delete book | ✅ |

</details>

<details>
<summary>💬 Comments Endpoints</summary>

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/books/{bookId}/comments` | Get all comments from a book | ❌ |
| `GET` | `/books/{bookId}/comments/{id}` | Get comment by ID | ❌ |
| `POST` | `/books/{bookId}/comments` | Create new comment | ✅ |
| `PUT` | `/books/{bookId}/comments/{id}` | Update comment | ✅ |
| `DELETE` | `/books/{bookId}/comments/{id}` | Delete comment | ✅ |

</details>

<details>
<summary>👤 Users Endpoints</summary>
  
| Method | Endpoint | Description | Auth | Admin |
|--------|----------|-------------|------|-------|
| `POST` | `/register` | Register new user | ❌ | ❌ |
| `POST` | `/login` | Login user | ❌ | ❌ |
| `POST` | `/refresh-token` | Refresh JWT token | ✅ | ❌ |
| `POST` | `/make-admin` | Grant admin privileges | ✅ | ✅ |

</details>

---

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server

### Installation

1. **Clone the repository**  
   `git clone https://github.com/weizheng2/aspnet-library-api.git`

2. **Configure application settings**  
   Update your appsettings.json, environment variables, or user-secrets with the required keys:  
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Your SQL Server connection string",
       "Redis": "Optional Redis connection string"
     }
   }
   ```
   
3. **Run and test the application locally**  
   `dotnet run`  
   `http://localhost:5177/swagger/index.html`
