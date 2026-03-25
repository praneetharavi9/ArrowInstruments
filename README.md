# ArrowInstruments 🎵

A full-stack web application for a manufacturer of professional instruments — built with ASP.NET Core Web API and Angular/TypeScript, featuring a product catalog, secure authentication, and complete CRUD operations backed by a relational database.

---

## 🚀 Tech Stack

**Backend**
- ASP.NET Core Web API (C#)
- Entity Framework Core
- SQL Server / PostgreSQL
- JWT Authentication & Role-Based Authorization
- RESTful API design

**Frontend**
- Angular with TypeScript
- HTML5 & CSS3
- Responsive UI design

---

## ✨ Features

- 🔐 **Secure Authentication** — JWT-based login with role-based access control
- 📦 **Product Catalog** — Browse and filter instruments by category
- 📝 **CRUD Operations** — Full create, read, update, delete for product management
- 📬 **Contact Form** — Customer inquiry submission
- 🔗 **RESTful API** — Clean, documented API endpoints consumed by the Angular frontend
- 🗄️ **Database Integration** — Persistent data storage with SQL Server/PostgreSQL via Entity Framework Core

---

## 🏗️ Architecture

```
ArrowInstruments/
├── Backend/                  # ASP.NET Core Web API
│   ├── Controllers/          # API endpoint controllers
│   ├── Models/               # Entity models
│   ├── Services/             # Business logic layer
│   ├── Data/                 # DbContext & migrations
│   └── appsettings.json      # Configuration
│
└── frontend/                 # Angular TypeScript app
    ├── src/
    │   ├── app/
    │   │   ├── components/   # UI components
    │   │   ├── services/     # API service layer
    │   │   └── models/       # TypeScript interfaces
    │   └── environments/     # Environment configs
    └── angular.json
```

---

## ⚙️ Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)
- SQL Server or PostgreSQL

### Backend Setup

```bash
# Navigate to backend
cd Backend

# Restore dependencies
dotnet restore

# Update appsettings.json with your connection string
# "ConnectionStrings": { "DefaultConnection": "your-connection-string" }

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run
```

API will be available at `https://localhost:7001`

### Frontend Setup

```bash
# Navigate to frontend
cd frontend

# Install dependencies
npm install

# Start the development server
ng serve
```

App will be available at `http://localhost:4200`

---

## 🔑 API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | User login | No |
| POST | `/api/auth/register` | User registration | No |
| GET | `/api/products` | Get all products | No |
| GET | `/api/products/{id}` | Get product by ID | No |
| POST | `/api/products` | Create product | Yes |
| PUT | `/api/products/{id}` | Update product | Yes |
| DELETE | `/api/products/{id}` | Delete product | Yes |
| POST | `/api/contact` | Submit contact form | No |

---

## 🔒 Authentication

The application uses **JWT (JSON Web Tokens)** for secure authentication:
- Tokens are issued on successful login
- Protected endpoints require a valid Bearer token
- Role-based access control restricts admin operations

---

## 📸 Screenshots

> _Add screenshots of your app here_
> 
> Example:
> ```
> ![Home Page](screenshots/home.png)
> ![Product Catalog](screenshots/catalog.png)
> ![Admin Dashboard](screenshots/admin.png)
> ```

---

## 🛣️ Roadmap

- [ ] Deploy to Azure App Service
- [ ] Add product search and filtering
- [ ] Implement shopping cart
- [ ] Add admin dashboard
- [ ] Write unit tests with NUnit

---

## 👩‍💻 Author

**Praneetha Ravi**  
Full-Stack Developer | .NET · Angular · AWS · Azure  
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=flat&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/praneetharavi/)
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=flat&logo=github&logoColor=white)](https://github.com/praneetharavi9)

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
