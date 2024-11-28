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
            Console.Clear();
            LogoHandler.DisplayHeader(_sqlConnection.Database);
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold underline rgb(190,40,0)]Data Management[/]")
                        .AddChoices("Insert Data", "Update Data", "Delete Data", "[red]Back[/]")
                );

                //Console.Clear();
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

            // Fetch columns and their data types to display to the user
            string columnsQuery = $@"
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMNPROPERTY(object_id(TABLE_NAME), COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = @TableName";
            var columns = new List<(string Name, string DataType, bool IsNullable, bool IsIdentity)>();

            try
            {
                using (var command = new SqlCommand(columnsQuery, _sqlConnection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES", reader.GetInt32(3) == 1));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving column names: {ex.Message}[/]");
                return;
            }

            if (columns.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns found for the selected table.[/]");
                return;
            }

            // Filter out auto-increment columns
            var nonIdentityColumns = columns.Where(c => !c.IsIdentity).ToList();

            var nonNullableColumns = nonIdentityColumns.Where(c => !c.IsNullable).Select(c => $"{c.Name} ({c.DataType})").ToList();
            var nullableColumns = nonIdentityColumns.Where(c => c.IsNullable).Select(c => $"{c.Name} ({c.DataType})").ToList();

            var nonNullableColumnsDisplay = string.Join(", ", nonNullableColumns.Select(c => $"[green]{c}[/]"));
            AnsiConsole.MarkupLine($"[yellow]The following columns are required and cannot be deselected:[/]");
            AnsiConsole.MarkupLine(nonNullableColumnsDisplay);

            var prompt = new MultiSelectionPrompt<string>()
                .Title("[bold yellow]Select optional columns to insert data into:[/]")
                .InstructionsText("[grey](Press [bold]<space>[/] to toggle a column, and [bold]<enter>[/] to confirm your selection)[/]")
                .NotRequired()
                .AddChoices(nullableColumns); // Only allow selection of nullable columns

            // Get user-selected columns
            var selectedOptionalColumns = AnsiConsole.Prompt(prompt);

            // Combine non-changeable and user-selected columns
            var selectedColumns = nonNullableColumns.Concat(selectedOptionalColumns).ToList();

            // Display final selection to the user
            AnsiConsole.MarkupLine($"[yellow]The following columns will be used for data insertion:[/]");
            AnsiConsole.MarkupLine(string.Join(", ", selectedColumns.Select(c => $"[green]{c}[/]")));

            // Get user selections
            if (selectedColumns.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns selected. Please try again.[/]");
                return;
            }

            var columnValues = new Dictionary<string, object>();

            foreach (var column in selectedColumns)
            {
                var columnName = column.Split(' ')[0];
                var dataType = columns.First(c => c.Name == columnName).DataType;

                // Prompt for value input
                string value = AnsiConsole.Ask<string>($"Enter the value for [bold]{columnName}[/] ([italic]{dataType}[/]): ");

                // Parse the input to the appropriate data type
                object parsedValue = ParseValue(value, dataType);
                if (parsedValue == null)
                {
                    AnsiConsole.MarkupLine($"[red]Invalid input for {columnName} of type {dataType}. Please try again.[/]");
                    return;
                }

                columnValues.Add(columnName, parsedValue);
            }

            string columnsInput = string.Join(", ", columnValues.Keys);
            string parametersInput = string.Join(", ", columnValues.Keys.Select(k => $"@{k}"));

            string query = $"INSERT INTO {tableName} ({columnsInput}) VALUES ({parametersInput})";

            try
            {
                using (var command = new SqlCommand(query, _sqlConnection))
                {
                    foreach (var (columnName, value) in columnValues)
                    {
                        command.Parameters.AddWithValue($"@{columnName}", value ?? DBNull.Value);
                    }

                    await command.ExecuteNonQueryAsync();

                    Console.Clear();
                    LogoHandler.DisplayHeader(_sqlConnection.Database);

                    AnsiConsole.MarkupLine($"[green]Data inserted successfully into {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error inserting data: {ex.Message}[/]");
            }

        }

        private object ParseValue(string input, string dataType)
        {
            try
            {
                return dataType.ToLower() switch
                {
                    "int" => int.Parse(input),
                    "bigint" => long.Parse(input),
                    "decimal" => decimal.Parse(input),
                    "float" => double.Parse(input),
                    "bit" => input.ToLower() == "true" || input == "1",
                    "datetime" or "date" => DateTime.Parse(input),
                    "nvarchar" or "varchar" or "text" => input,
                    "binary" or "varbinary" => Convert.FromBase64String(input), // Assuming input is base64 encoded
                    _ => input // Default fallback
                };
            }
            catch
            {
                return null; // Return null if parsing fails
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
