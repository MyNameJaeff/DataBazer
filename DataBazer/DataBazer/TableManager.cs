using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class TableManager
    {
        private readonly SqlConnection _sqlConnection;

        public TableManager(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task HandleTableManagement()
        {
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Table Management[/]")
                        .AddChoices("Create Table", "Modify Table", "Drop Table", "[red]Back[/]")
                );

                switch (selection)
                {
                    case "Create Table":
                        await CreateTable();
                        break;

                    case "Modify Table":
                        await ModifyTable();
                        break;

                    case "Drop Table":
                        await DropTable();
                        break;

                    case "[red]Back[/]":
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid option.[/]");
                        break;
                }
            }
        }

        private async Task CreateTable()
        {
            // Implementation for creating a table
        }

        private async Task ModifyTable()
        {
            // Implementation for modifying a table
        }

        private async Task DropTable()
        {
            // Implementation for dropping a table
        }
    }
}
