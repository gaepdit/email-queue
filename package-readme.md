# NuGet Package maintenance

Some transitive NuGet packages have been updated directly to work around vulnerable dependencies in other packages.

- `Microsoft.OpenApi` 2.11.0 has added to `EmailQueue.API` to avoid a vulnerable version in
  `Microsoft.AspNetCore.OpenApi`.

- `SQLitePCLRaw.lib.e_sqlite3` 2.1.12 has added to `EmailQueue.API` to avoid a vulnerable version in
  `Microsoft.EntityFrameworkCore.Sqlite`.