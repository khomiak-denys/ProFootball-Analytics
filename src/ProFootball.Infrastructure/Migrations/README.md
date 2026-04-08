# Migration Placeholder

This folder is intentionally tracked for EF Core migrations.

Create the first migration with:

```powershell
dotnet ef migrations add InitialCreate \
  --project .\src\ProFootball.Infrastructure\ProFootball.Infrastructure.csproj \
  --startup-project .\src\ProFootball.Presentation\ProFootball.Presentation.csproj
```

Apply it with:

```powershell
dotnet ef database update \
  --project .\src\ProFootball.Infrastructure\ProFootball.Infrastructure.csproj \
  --startup-project .\src\ProFootball.Presentation\ProFootball.Presentation.csproj
```
