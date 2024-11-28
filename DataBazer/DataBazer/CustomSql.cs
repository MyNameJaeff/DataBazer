using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class CustomSql
    {
        private readonly SqlConnection _sqlConnection;

        public CustomSql(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task HandleCustomSql()
        {
            Console.Clear();
            LogoHandler.DisplayHeader(_sqlConnection.Database);
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold underline rgb(190,40,0)]Custom SQL[/]")
                        .AddChoices("Execute SQL Query", "[red]Back[/]")
                );

                Console.Clear();

                switch (selection)
                {
                    case "Execute SQL Query":
                        await ExecuteSqlQuery();
                        break;

                    case "[red]Back[/]":
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid option.[/]");
                        break;
                }
            }
        }

        private async Task ExecuteSqlQuery()
        {
            AnsiConsole.MarkupLine("[bold]Enter your SQL query:[/]");
            var query = AnsiConsole.Ask<string>("");

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine("[green]Query executed successfully.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error executing query:[/] {ex.Message}");
            }

            AnsiConsole.MarkupLine("[bold]Press [green]Enter[/] to continue...[/]");
            Console.ReadLine();
        }
    }
}
