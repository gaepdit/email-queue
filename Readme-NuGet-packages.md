# NuGet Package maintenance

The following NuGet packages have been added directly to work around vulnerable dependencies in other packages.

- `Azure.Identity` was added to resolve an ambiguity between `Azure.Identity` and `Azure.Core` introduced by the addition of `Microsoft.EntityFrameworkCore.SqlServer`.

- `Microsoft.Testing.Platform` was added to `EmailQueue.API.Tests` to resolve a version conflict introduced by an update to `NunitXml.TestLogger`.

- `SQLitePCLRaw.lib.e_sqlite3` was added to avoid a vulnerable version referenced in `EfCore.TestSupport` (via `Microsoft.EntityFrameworkCore.Sqlite`).

- `System.Security.Cryptography.Pkcs` was added to avoid a vulnerable version referenced in `Microsoft.Data.SqlClient`.
