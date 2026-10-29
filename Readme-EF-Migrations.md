### Entity Framework database migrations

There are two migration assemblies for use with a SQLite or SQL Server database, respectively. To add a new Entity
Framework database migration, the migration command must be run for both database providers.

1. Open a command prompt to the solution root folder.

2. Run the following commands with an appropriate migration name:

   ```
   dotnet ef migrations add NAME_OF_MIGRATION --project .\src\EmailQueue.SqliteMigrations\
   dotnet ef migrations add NAME_OF_MIGRATION --project .\src\EmailQueue.SqlServerMigrations\
   ```