using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await ConnectToDatabase();
        }

        static async Task ConnectToDatabase()
        {
            var dbManager = new DatabaseManager();
            SqlConnection? sqlConnection = null;

            while (sqlConnection == null)
            {
                sqlConnection = await dbManager.HandleDatabaseSelection();

                if (sqlConnection == null)
                {
                    AnsiConsole.MarkupLine("[red]Failed to connect to a database. Please try again.[/]");
                }
            }

            await MainMenu(sqlConnection);
        }

        private static async Task MainMenu(SqlConnection sqlConnection)
        {
            while (true)
            {
                Console.Clear();
                LogoHandler.DisplayHeader(sqlConnection.Database);
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold underline rgb(190,40,0)]Main Menu[/]")
                        .AddChoices("Table Management", "Data Management", "Index Management", "View Data", "Custom SQL", "[red]Back[/]", "[red]Exit[/]")
                );

                switch (selection)
                {
                    case "Table Management":
                        var tableManager = new TableManager(sqlConnection);
                        await tableManager.HandleTableManagement();
                        break;

                    case "Data Management":
                        var dataManager = new DataManager(sqlConnection);
                        await dataManager.HandleDataOperations();
                        break;

                    case "Index Management":
                        var indexManager = new IndexManager(sqlConnection);
                        await indexManager.HandleIndexManagement();
                        break;

                    case "View Data":
                        var dataViewer = new DataViewer(sqlConnection);
                        await dataViewer.ViewDataManager();
                        break;

                    case "Custom SQL":
                        var customSql = new CustomSql(sqlConnection);
                        await customSql.HandleCustomSql();
                        break;

                    case "[red]Back[/]":
                        Console.Clear();
                        await ConnectToDatabase();
                        return;

                    case "[red]Exit[/]":
                        AnsiConsole.MarkupLine("[green]Goodbye![/]");
                        sqlConnection?.Close();
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid option, please try again.[/]");
                        break;
                }
            }
        }
    }
}
