# Lanflix migration tooling

This project is intentionally excluded from the production Host dependency graph.
Create migrations from the server directory with:

```powershell
dotnet ef migrations add <Name> --project app/Infrastructure --startup-project tools/Migrations
```

The generated migration files remain owned by `Lanflix.Infrastructure`; the Host
applies them transactionally during startup.
