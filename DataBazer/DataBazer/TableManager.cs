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
            Console.Clear();
            LogoHandler.DisplayHeader(_sqlConnection.Database);
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold underline rgb(190,40,0)]Table Management[/]")
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
            // Step 1: Get Existing Tables
            List<string> existingTables = await GetExistingTableNames();
            if (existingTables == null)
            {
                AnsiConsole.MarkupLine("[red]Unable to fetch existing tables. Please try again later.[/]");
                return;
            }

            // Step 2: Get Table Name
            string tableName = AnsiConsole.Ask<string>("[yellow]Enter the name of the new table:[/]");
            if (string.IsNullOrWhiteSpace(tableName))
            {
                AnsiConsole.MarkupLine("[red]Table name cannot be empty.[/]");
                return;
            }
            if (existingTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]A table named '{tableName}' already exists.[/]");
                return;
            }

            // Step 3: Define Columns
            var columns = new List<string>();
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasPrimary = false;

            // List of SQL Server data types
            var sqlDataTypes = new List<string>
    {
        "Custom (enter manually)",
        "bigint", "binary", "bit", "char", "date", "datetime", "datetime2", "datetimeoffset", "decimal",
        "float", "image", "int", "money", "nchar", "ntext", "numeric", "nvarchar", "real", "smalldatetime",
        "smallint", "smallmoney", "sql_variant", "text", "time", "timestamp", "tinyint", "uniqueidentifier",
        "varbinary", "varchar", "xml"
    };

            while (true)
            {
                var columnName = AnsiConsole.Ask<string>("[cyan]Enter the column name (or type 'done' to finish):[/]");
                if (string.Equals(columnName, "done", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(columnName))
                {
                    AnsiConsole.MarkupLine("[red]Column name cannot be empty.\n[/]");
                    continue;
                }

                if (columnNames.Contains(columnName))
                {
                    AnsiConsole.MarkupLine($"[red]A column named '{columnName}' already exists in this table.\n[/]");
                    continue;
                }

                // Step 3.1: Select or define data type
                var selectedDataType = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select the data type:[/]")
                        .AddChoices(sqlDataTypes));

                string dataType;
                if (selectedDataType == "Custom (enter manually)")
                {
                    dataType = AnsiConsole.Ask<string>("[cyan]Enter the custom data type:[/]");
                    if (string.IsNullOrWhiteSpace(dataType))
                    {
                        AnsiConsole.MarkupLine("[red]Custom data type cannot be empty. Please try again.\n[/]");
                        continue;
                    }
                }
                else
                {
                    dataType = selectedDataType;
                    AnsiConsole.MarkupLine($"[cyan]Data type selected: {dataType}[/]");
                }

                // Step 3.2: Primary Key & Constraints
                bool isPrimaryKey = false;
                if (!hasPrimary)
                {
                    isPrimaryKey = AnsiConsole.Confirm("[cyan]Is this column a Primary Key?[/]");
                }

                bool isAutoIncrement = false;
                if (isPrimaryKey && dataType == "int")
                {
                    isAutoIncrement = AnsiConsole.Confirm("[cyan]Should this column auto-increment?[/]");
                }

                bool isNullable = !isPrimaryKey && AnsiConsole.Confirm("[cyan]Allow NULL values?[/]");

                // Build the column definition
                string columnDefinition = $"{columnName} {dataType}";
                if (isPrimaryKey)
                {
                    columnDefinition += " PRIMARY KEY";
                    hasPrimary = true;
                }
                if (isAutoIncrement)
                {
                    columnDefinition += " IDENTITY(1,1)";
                }
                if (!isNullable)
                {
                    columnDefinition += " NOT NULL";
                }

                columns.Add(columnDefinition);
                columnNames.Add(columnName); // Track the column name

                Console.WriteLine(); // Readability
            }

            if (columns.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns defined. Table creation canceled.\n[/]");
                return;
            }

            // Step 4: Generate SQL
            string createTableSql = $"CREATE TABLE {tableName} (\n    {string.Join(",\n    ", columns)}\n);";

            // Preview SQL to user
            AnsiConsole.MarkupLine("[yellow]The following SQL will be executed:[/]");
            AnsiConsole.Write(new Panel(createTableSql).BorderColor(Color.Green));

            if (!AnsiConsole.Confirm("[cyan]Do you want to execute this SQL?[/]"))
            {
                Console.Clear();
                LogoHandler.DisplayHeader(_sqlConnection.Database);
                AnsiConsole.MarkupLine("[red]Table creation canceled.[/]");
                return;
            }

            // Step 5: Execute SQL
            try
            {
                await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("[yellow]Creating table...[/]", async ctx =>
                {
                    await Task.Delay(1500); // Simulate loading
                    try
                    {
                        using (var command = new SqlCommand(createTableSql, _sqlConnection))
                        {
                            await command.ExecuteNonQueryAsync();
                            Console.Clear();
                            LogoHandler.DisplayHeader(_sqlConnection.Database);
                            AnsiConsole.MarkupLine($"[green]Table '{tableName}' created successfully![/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        ctx.Status($"[red]Error: {ex.Message}[/]");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error creating table: {ex.Message}[/]");

                // Ask if the user wants to try again
                var retry = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Would you like to try again?[/]")
                        .AddChoices("Yes", "No (Go back)")
                        .HighlightStyle("cyan"));

                if (retry == "Yes")
                {
                    await CreateTable(); // Retry the process
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Returning to Table Management menu.[/]");
                }
            }
        }


        private async Task<List<string>> GetExistingTableNames()
        {
            var tableNames = new List<string>();

            try
            {
                const string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
                using (var command = new SqlCommand(query, _sqlConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
                tableNames.Sort(); // Sort the table names
                await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("[yellow]Fetching tables...[/]", async ctx =>
                {
                    await Task.Delay(1500); // Simulate loading
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching table names: {ex.Message}[/]");
            }

            return tableNames;
        }

        private async Task ModifyTable()
        {
            Console.Clear();
            LogoHandler.DisplayHeader(_sqlConnection.Database);
            AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
            return;
        }

        private async Task DropTable()
        {
            List<string> existingTables = await GetExistingTableNames();

            if (existingTables == null)
            {
                AnsiConsole.MarkupLine("[red]Unable to fetch existing tables. Please try again later.[/]");
                return;
            }

            if (existingTables.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            existingTables.Add("[red]Cancel[/]");

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to drop:[/]")
                    .AddChoices(existingTables));

            if (tableName == "[red]Cancel[/]")
            {
                return;
            }

            if (!AnsiConsole.Confirm($"[red]Are you sure you want to drop the table '{tableName}'?[/]"))
            {
                return;
            }

            string dropTableSql = $"DROP TABLE {tableName};";

            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("[yellow]Dropping table...[/]", async ctx =>
                    {
                        await Task.Delay(1500); // Simulate loading
                        try
                        {
                            using (var command = new SqlCommand(dropTableSql, _sqlConnection))
                            {
                                await command.ExecuteNonQueryAsync();
                                Console.Clear();
                                LogoHandler.DisplayHeader(_sqlConnection.Database);
                                AnsiConsole.MarkupLine($"[green]Table '{tableName}' dropped successfully![/]");
                            }
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"[red]Error: {ex.Message}[/]");
                            throw;
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error dropping table: {ex.Message}[/]");
            }
        }
    }
}
