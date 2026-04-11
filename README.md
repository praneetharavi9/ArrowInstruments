# Arrow Instruments

Product catalog and enquiry management website for Arrow Instruments — an industrial instrumentation company based in India.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core 8, EF Core 9 (Pomelo MySQL) |
| Frontend | Angular 20, Bootstrap 5.3, Font Awesome 7 |
| Database | MySQL 8 (hosted on HostGator) |
| Image storage | FTP to HostGator `public_html/product_images/` |
| Email | MailKit / SMTP (HostGator mail server) |
| Auth | JWT Bearer tokens, BCrypt password hashing |

## Project Structure

```
ArrowInstruments/
├── Backend/                  # ASP.NET Core 8 Web API
│   ├── Controllers/          # ProductsController, ProductTypeController,
│   │                         #   AuthController, ContactController, EnquiriesController
│   ├── Data/AppDbContext.cs   # EF Core context
│   ├── Models/               # EF models + settings POCOs
│   ├── Repository/           # Repository pattern (products, product types)
│   ├── appsettings.json      # Base config — PLACEHOLDER values only (committed)
│   ├── appsettings.Development.json   # Real dev credentials (git-ignored)
│   └── appsettings.Production.json    # Real prod credentials (git-ignored)
│
└── frontend/                 # Angular 20 SPA
    └── src/
        ├── app/
        │   ├── home/         # Landing page
        │   ├── products/     # Public product catalog
        │   ├── about/        # About page
        │   ├── contact/      # Contact / enquiry form
        │   ├── mega-menu/    # Product category navigation
        │   └── admin/        # Lazy-loaded admin portal
        │       ├── login/    # Admin login
        │       ├── dashboard/
        │       ├── product-types/
        │       ├── admin-products/
        │       └── enquiries/
        └── environments/
            ├── environment.ts             # Production config
            └── environment.development.ts # Dev config (ng serve)
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Node.js 20+ and npm
- Access credentials for MySQL, FTP, and SMTP (see [SECRETS.md](SECRETS.md))

## Local Development Setup

### 1. Backend

```bash
cd Backend

# Create your local secrets file (git-ignored):
cp appsettings.json appsettings.Development.json
# Edit appsettings.Development.json with real DB / FTP / email / JWT values.

dotnet restore
dotnet run
# API available at https://localhost:7275
```

### 2. Frontend

```bash
cd frontend
npm install
npm start
# App available at http://localhost:4200
```

`ng serve` uses `environment.development.ts` automatically via `fileReplacements` in `angular.json`.

## Production Build

### Backend

```bash
cd Backend
dotnet publish -c Release -o ./publish
```

Upload the contents of `Backend/publish/` to your hosting environment.
Set the `JWT_SECRET` environment variable (or fill `appsettings.Production.json`) before starting.

### Frontend

```bash
cd frontend
npm run build:prod
```

Output is written to `frontend/dist/frontend/browser/`. Upload to `public_html/` on HostGator.

## Environment Variables

| Variable | Where used | Purpose |
|---|---|---|
| `JWT_SECRET` | Backend (env var) | Overrides `JwtSettings.Secret` at runtime |

All other credentials live in `appsettings.Development.json` (local) or `appsettings.Production.json` (server). Neither file is committed to git. See [SECRETS.md](SECRETS.md) for the full list.

## Admin Portal

The admin portal is at `/admin/login`.

- JWT-based login with BCrypt password verification
- Product type (category) CRUD
- Product CRUD with FTP image upload
- Enquiry management (view, mark responded, delete)

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/admin/login` | — | Admin login |
| GET | `/api/auth/admin/verify` | Admin | Verify token |
| GET | `/api/producttype` | — | List all categories |
| POST | `/api/producttype` | Admin | Create category |
| PUT | `/api/producttype/{id}` | Admin | Update category |
| DELETE | `/api/producttype/{id}` | Admin | Delete category (cascades) |
| GET | `/api/products` | — | List products (optional `?typeId=`) |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/{id}` | Admin | Update product |
| DELETE | `/api/products/{id}` | Admin | Delete product |
| POST | `/api/products/{id}/image` | Admin | Upload product image via FTP |
| POST | `/api/contact` | — | Submit enquiry |
| GET | `/api/enquiries` | Admin | List enquiries |
| PUT | `/api/enquiries/{id}/status` | Admin | Update enquiry status |
| DELETE | `/api/enquiries/{id}` | Admin | Delete enquiry |
