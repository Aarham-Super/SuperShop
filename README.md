# SuperShop

SuperShop is a demonstration ASP.NET Core MVC shop using MySQL. Demo orders do not process payments.

## Run locally

1. Configure MySQL using `ConnectionStrings:DefaultConnection` or the `MYSQL_CONNECTION_STRING` environment variable. Alternatively set `MYSQL_HOST`, `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD`, and optionally `MYSQL_PORT`.
2. Run `dotnet run --launch-profile http` from this project folder. The app uses `http://localhost:3000`.
3. EF Core applies pending database migrations at startup.

Keep database credentials out of source control. For local development, use `dotnet user-secrets` or environment variables.

## Password reset email

Password recovery emails require a working SMTP service. Set these values through user secrets or the hosting provider's private environment settings; do not commit the values:

- `Email:Host`
- `Email:Port` (normally 587)
- `Email:Username`
- `Email:Password`
- `Email:FromAddress`
- `Email:FromName` (optional)
- `Email:EnableSsl` (normally `true`)

The reset code is random, expires after 10 minutes, is stored only as a salted hash, allows up to five attempts, and is consumed once. A new code invalidates previous codes. Password reset requests return a generic confirmation to avoid exposing whether an email has an account.

## GitHub

Before the first push, create a private or public repository and set its URL as the remote. Example (replace the placeholder):

```powershell
git remote add origin https://github.com/<YOUR_ACCOUNT>/<YOUR_REPOSITORY>.git
git push -u origin main
```

Review the repository and its ignored files before publishing. Never include user-secrets, local environment files, database passwords, or SMTP credentials.
