# NuGet Package maintenance

The following NuGet packages have been added directly to work around vulnerable dependencies in other packages.

- `Microsoft.Testing.Platform` was added to `EmailQueue.API.Tests` to resolve a version conflict introduced by an update to `NunitXml.TestLogger`.

- `SQLitePCLRaw.lib.e_sqlite3` was added to avoid a vulnerable version referenced in `EfCore.TestSupport` (via `Microsoft.EntityFrameworkCore.Sqlite`).

- `System.Security.Cryptography.Pkcs` was added to avoid a vulnerable version referenced in `Microsoft.Data.SqlClient`.
