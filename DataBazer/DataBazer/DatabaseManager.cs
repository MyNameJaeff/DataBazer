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
                LogoHandler.DisplayLogo();
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold underline rgb(190,40,0)]Database Management[/]")
                        .AddChoices("Pick Database", "Create Database", "Delete Database", "[red]Exit[/]")
                        .HighlightStyle("cyan"));

                //Console.Clear();

                switch (selection)
                {
                    case "Pick Database":
                        return await SelectDatabase();

                    case "Create Database":
                        return await CreateDatabase();

                    case "Delete Database":
                        return await DeleteDatabase();

                    case "[red]Exit[/]":
                        AnsiConsole.MarkupLine("[green bold]Goodbye![/]");
                        Environment.Exit(0);
                        break;

                    default:
                        AnsiConsole.MarkupLine("[red bold]Invalid choice, try again.[/]");
                        break;
                }
            }
        }

        private async Task<SqlConnection?> SelectDatabase()
        {
            List<string> databases = new List<string>();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("[yellow]Fetching available databases...[/]", async ctx =>
                {
                    await Task.Delay(1500); // Simulate loading
                    ctx.Status("[yellow]Connecting to the server...[/]");

                    try
                    {
                        using (var serverConnection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                        {
                            serverConnection.Open();
                            databases = await GetAllDatabases(serverConnection);
                        }
                    }
                    catch (Exception ex)
                    {
                        ctx.Status($"[red]Error: {ex.Message}[/]");
                        throw;
                    }
                });

            if (databases.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No databases found.[/]");
                return null;
            }

            databases.Add("[red]Back[/]");

            var selectedDatabase = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a database to connect:[/]")
                    .AddChoices(databases)
                    .HighlightStyle("cyan"));

            if (selectedDatabase == "[red]Back[/]")
            {
                Console.Clear();
                return await HandleDatabaseSelection();
            }

            return await GetDatabase(selectedDatabase);
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
            while (true)
            {
                string dbName = AnsiConsole.Ask<string>("[yellow]Enter the name of the new database:[/]");

                if (string.IsNullOrWhiteSpace(dbName))
                {
                    AnsiConsole.MarkupLine("[red bold]Invalid database name.[/]");
                    continue;
                }

                const string queryTemplate = "CREATE DATABASE [{0}]";

                try
                {
                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("[yellow]Creating database...[/]", async ctx =>
                        {
                            await Task.Delay(1500); // Simulate loading
                            ctx.Status("[yellow]Connecting to the server...[/]");

                            using (var connection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                            {
                                connection.Open();
                                using (var command = new SqlCommand(string.Format(queryTemplate, dbName), connection))
                                {
                                    await command.ExecuteNonQueryAsync();
                                    AnsiConsole.MarkupLine($"[green bold]Database [yellow]{dbName}[/] created successfully.[/]");
                                }
                            }
                        });

                    var question = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"[yellow]Do you want to connect to [bold]{dbName}[/]?:[/]")
                            .AddChoices("Yes", "No (Go back)")
                            .HighlightStyle("cyan"));

                    if (question == "No (Go back)")
                    {
                        Console.Clear();
                        return await HandleDatabaseSelection();
                    }

                    return await GetDatabase(dbName);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    var retryQuestion = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[yellow]Do you want to try again?:[/]")
                            .AddChoices("Yes", "No (Go back)")
                            .HighlightStyle("cyan"));

                    if (retryQuestion == "No (Go back)")
                    {
                        Console.Clear();
                        return await HandleDatabaseSelection();
                    }
                }
            }
        }


        private async Task<SqlConnection?> DeleteDatabase()
        {
            while (true)
            {
                try
                {
                    List<string> databases = new List<string>();

                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("[yellow]Fetching available databases...[/]", async ctx =>
                        {
                            await Task.Delay(1500); // Simulate loading
                            ctx.Status("[yellow]Connecting to the server...[/]");

                            try
                            {
                                using (var serverConnection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                                {
                                    serverConnection.Open();
                                    databases = await GetAllDatabases(serverConnection);
                                }
                            }
                            catch (Exception ex)
                            {
                                ctx.Status($"[red]Error: {ex.Message}[/]");
                                throw;
                            }
                        });

                    if (databases.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No databases found.[/]");
                        return null;
                    }

                    databases.Add("[red]Back[/]");

                    var selectedDatabase = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[yellow]Select a database to delete:[/]")
                            .AddChoices(databases)
                            .HighlightStyle("cyan"));

                    if (selectedDatabase == "[red]Back[/]")
                    {
                        Console.Clear();
                        return await HandleDatabaseSelection();
                    }

                    if (selectedDatabase == "master")
                    {
                        AnsiConsole.MarkupLine("[red bold]You cannot delete the master database.[/]");
                        continue;
                    }

                    const string queryTemplate = "DROP DATABASE [{0}]";

                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("[yellow]Deleting database...[/]", async ctx =>
                        {
                            await Task.Delay(1500); // Simulate loading
                            ctx.Status("[yellow]Connecting to the server...[/]");

                            using (var connection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                            {
                                connection.Open();
                                using (var command = new SqlCommand(string.Format(queryTemplate, selectedDatabase), connection))
                                {
                                    await command.ExecuteNonQueryAsync();
                                    AnsiConsole.MarkupLine($"[green bold]Database [yellow]{selectedDatabase}[/] deleted successfully.[/]");
                                }
                            }
                        });

                    var question = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[yellow]Do you want to delete another database?:[/]")
                            .AddChoices("Yes", "No (Go back)")
                            .HighlightStyle("cyan"));

                    if (question == "No (Go back)")
                    {
                        Console.Clear();
                        return await HandleDatabaseSelection();
                    }

                    Console.Clear();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    var retryQuestion = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[yellow]Do you want to try again?:[/]")
                            .AddChoices("Yes", "No (Go back)")
                            .HighlightStyle("cyan"));

                    if (retryQuestion == "No (Go back)")
                    {
                        Console.Clear();
                        return await HandleDatabaseSelection();
                    }
                }
            }
        }

        private async Task<SqlConnection> GetDatabase(string dbName)
        {
            string connectionString = @$"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True";
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
