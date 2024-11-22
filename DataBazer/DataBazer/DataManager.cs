using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class DataManager
    {
        private readonly SqlConnection _sqlConnection;

        public DataManager(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task HandleDataOperations()
        {
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Data Management[/]")
                        .AddChoices("Insert Data", "Update Data", "Delete Data", "[red]Back[/]")
                );

                switch (selection)
                {
                    case "Insert Data":
                        await InsertData();
                        break;

                    case "Update Data":
                        await UpdateData();
                        break;

                    case "Delete Data":
                        await DeleteData();
                        break;

                    case "[red]Back[/]":
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid option.[/]");
                        break;
                }
            }
        }

        // Helper to get all table names in the current database
        private async Task<List<string>> GetAllTableNames()
        {
            var tableNames = new List<string>();
            const string query = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'";

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving table names: {ex.Message}[/]");
            }

            return tableNames;
        }

        // Insert Data
        // Insert Data
        private async Task InsertData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to insert data into:[/]")
                    .AddChoices(tableNames)
            );

            // Fetch columns to display an example to the user
            string columnsQuery = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            var columnNames = new List<string>();

            try
            {
                using (var command = new SqlCommand(columnsQuery, _sqlConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        columnNames.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving column names: {ex.Message}[/]");
            }

            if (columnNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns found for the selected table.[/]");
                return;
            }

            // Show an example based on the columns
            string columnsExample = string.Join(", ", columnNames);
            string valuesExample = string.Join(", ", columnNames.Select(c => "'SampleValue'"));

            AnsiConsole.MarkupLine($"[green]Example: \n[/][bold]Columns[/] ({columnsExample})");
            AnsiConsole.MarkupLine($"[green]Values[/] ({valuesExample})");

            // Ask for input
            string columnsInput = AnsiConsole.Ask<string>("Enter the columns: ");
            string valuesInput = AnsiConsole.Ask<string>("Enter the values: ");

            string query = $"INSERT INTO {tableName} ({columnsInput}) VALUES ({valuesInput})";

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data inserted successfully into {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error inserting data: {ex.Message}[/]");
            }
        }


        // Update Data
        private async Task UpdateData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to update data in:[/]")
                    .AddChoices(tableNames)
            );

            // Fetch column names to display an example to the user
            string columnsQuery = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            var columnNames = new List<string>();

            try
            {
                using (var command = new SqlCommand(columnsQuery, _sqlConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        columnNames.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving column names: {ex.Message}[/]");
            }

            if (columnNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns found for the selected table.[/]");
                return;
            }

            // Show an example based on the columns
            string columnsExample = string.Join(", ", columnNames);
            string setExample = string.Join(", ", columnNames.Select(c => $"{c} = 'SampleValue'"));

            AnsiConsole.MarkupLine($"[green]Example: [/][bold]Columns[/] ({columnsExample})");
            AnsiConsole.MarkupLine($"[green]SET Clause[/] ({setExample})");

            // Ask for user input
            string setClause = AnsiConsole.Ask<string>("Enter the SET clause (e.g., Name = 'John'): ");
            string condition = AnsiConsole.Ask<string>("Enter the condition (e.g., Id = 1): ");
            string query = $"UPDATE {tableName} SET {setClause} WHERE {condition}";

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data updated successfully in {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error updating data: {ex.Message}[/]");
            }
        }


        // Delete Data
        private async Task DeleteData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to delete data from:[/]")
                    .AddChoices(tableNames)
            );

            // Fetch column names to display an example to the user
            string columnsQuery = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            var columnNames = new List<string>();

            try
            {
                using (var command = new SqlCommand(columnsQuery, _sqlConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        columnNames.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving column names: {ex.Message}[/]");
            }

            if (columnNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns found for the selected table.[/]");
                return;
            }

            // Show an example based on the columns
            string columnsExample = string.Join(", ", columnNames);
            string conditionExample = $"{columnNames[0]} = 'SampleValue'"; // Just an example of condition

            AnsiConsole.MarkupLine($"[green]Example: [/][bold]Columns[/] ({columnsExample})");
            AnsiConsole.MarkupLine($"[green]Condition[/] ({conditionExample})");

            // Ask for user input
            string condition = AnsiConsole.Ask<string>("Enter the condition (e.g., Id = 1): ");
            string query = $"DELETE FROM {tableName} WHERE {condition}";

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data deleted successfully from {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error deleting data: {ex.Message}[/]");
            }
        }
    }
}
