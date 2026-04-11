# Secrets & Credentials

This file lists every secret used in the project, where to set it, and what it does.
**Never commit real credentials to git.**

---

## Backend (`Backend/appsettings.Development.json` or `appsettings.Production.json`)

Both files are git-ignored. Copy `appsettings.json` and fill in the values below.

| Key path | Description |
|---|---|
| `ConnectionStrings.DefaultConnection` | MySQL connection string (host, database, user, password, port) |
| `FtpSettings.Host` | FTP hostname (e.g. `ftp.arrowinstruments.in`) |
| `FtpSettings.User` | FTP username |
| `FtpSettings.Password` | FTP password |
| `JwtSettings.Secret` | JWT signing secret — must be at least 32 characters |
| `EmailSettings.SmtpHost` | SMTP hostname |
| `EmailSettings.SmtpUser` | SMTP username / email address |
| `EmailSettings.SmtpPass` | SMTP password |

### Runtime override (optional)

Set the `JWT_SECRET` environment variable to override `JwtSettings.Secret` without touching any file:

```bash
export JWT_SECRET="your-very-long-random-secret-here"
```

---

## Frontend (`frontend/src/environments/`)

`environment.ts` (production) and `environment.development.ts` (dev) are both committed, but they contain **no secrets** — only public API URLs and the image CDN base URL. No action needed.

---

## How to set up locally

1. Copy the base config:
   ```bash
   cp Backend/appsettings.json Backend/appsettings.Development.json
   ```
2. Open `Backend/appsettings.Development.json` and replace every `PLACEHOLDER` value with the real credential.
3. Run `dotnet run` — ASP.NET Core will automatically merge `appsettings.Development.json` on top of `appsettings.json` in the Development environment.

## How to set up on the production server

1. Create `Backend/appsettings.Production.json` on the server with real production credentials.
2. Alternatively, set environment variables that ASP.NET Core maps to config keys, e.g.:
   ```
   ConnectionStrings__DefaultConnection=Server=...
   JWT_SECRET=...
   ```
3. Ensure the file (if used) has restrictive permissions (readable by the app user only).
