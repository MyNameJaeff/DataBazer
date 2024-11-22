using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class DatabaseManager
    {
        public async Task<SqlConnection?> HandleDatabaseSelection()
        {
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Database Management[/]")
                        .AddChoices("Pick Database", "Create Database", "[red]Exit[/]")
                );

                switch (selection)
                {
                    case "Pick Database":
                        return await SelectDatabase();

                    case "Create Database":
                        return await CreateDatabase();

                    case "[red]Exit[/]":
                        return null;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid choice, try again.[/]");
                        break;
                }
            }
        }

        private async Task<SqlConnection?> SelectDatabase()
        {
            AnsiConsole.MarkupLine("[yellow]Fetching available databases...[/]");

            try
            {
                using (var serverConnection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                {
                    serverConnection.Open();
                    var databases = await GetAllDatabases(serverConnection);

                    if (databases.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No databases found.[/]");
                        return null;
                    }

                    var selectedDatabase = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[yellow]Select a database to connect:[/]")
                            .AddChoices(databases)
                    );

                    return GetDatabase(selectedDatabase);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return null;
            }
        }

        private async Task<List<string>> GetAllDatabases(SqlConnection serverConnection)
        {
            var databases = new List<string>();
            const string query = "SELECT name FROM sys.databases WHERE state = 0";

            try
            {
                using (var command = new SqlCommand(query, serverConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        databases.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving databases: {ex.Message}[/]");
            }

            return databases;
        }

        private async Task<SqlConnection?> CreateDatabase()
        {
            string dbName = AnsiConsole.Ask<string>("[yellow]Enter the name of the new database:[/]");

            if (string.IsNullOrWhiteSpace(dbName))
            {
                AnsiConsole.MarkupLine("[red]Invalid database name.[/]");
                return null;
            }

            const string queryTemplate = "CREATE DATABASE [{0}]";

            try
            {
                using (var connection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                {
                    connection.Open();
                    using (var command = new SqlCommand(string.Format(queryTemplate, dbName), connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        AnsiConsole.MarkupLine($"[green]Database {dbName} created successfully.[/]");
                        return GetDatabase(dbName);
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return null;
            }
        }

        private SqlConnection GetDatabase(string dbName)
        {
            string connectionString = @$"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True";
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}
