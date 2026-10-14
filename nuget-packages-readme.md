# NuGet Package maintenance

The following NuGet packages have been added directly to work around vulnerable dependencies in other packages.

- `SQLitePCLRaw.lib.e_sqlite3` was added and pinned to version 2.x to avoid a vulnerable version referenced in
  `EfCore.TestSupport` (via `Microsoft.EntityFrameworkCore.Sqlite`).
