### Entity Framework database migrations

Instructions for adding a new Entity Framework database migration and updating the database.

1. Open a command prompt to this folder (`.\src\EmailQueue.API\`).

2. Run the following command with an appropriate migration name:

   `dotnet ef migrations add NAME_OF_MIGRATION --msbuildprojectextensionspath ..\..\.artifacts\EmailQueue.API\obj\`

3. Follow the documentation to create deployment scripts and apply them on the
   server: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli

    1. Command to create SQL Scripts:

       `dotnet ef migrations script AddNewTables`

    2. Command to create an executable bundle (requires the `appsettings.json` file to be available alongside the
       generated bundle):

       `dotnet ef migrations bundle`
