using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class DataViewer
    {
        private readonly SqlConnection _sqlConnection;

        public DataViewer(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task ViewDataManager()
        {
            Console.Clear();
            LogoHandler.DisplayHeader(_sqlConnection.Database);
            while (true)
            {
                // Display the menu without clearing the screen at the start
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        //.HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]Data Viewing[/]")
                        .AddChoices("View Tables", "Filter Data", "Sort Data", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                // Clear the screen after the prompt if necessary
                //Console.Clear();

                switch (selection.Trim())
                {
                    case "View Tables":
                        await ViewData();
                        break;

                    case "Filter Data":
                        await FilterData();
                        break;

                    case "Sort Data":
                        await SortData();
                        break;

                    case "[red]Back to Main Menu[/]":
                        return; // Exit back to the main menu without clearing the screen again

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }

        // Get all table names from the database
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

        // View Data
        private async Task ViewData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to view data:[/]")
                    .AddChoices(tableNames));

            string query = $"SELECT * FROM {tableName}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table()
                        .BorderColor(Color.Green);  // Set border color for the table

                    // Add table headers based on database columns
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        table.AddColumn(reader.GetName(i));
                    }

                    // Add table rows from the data
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader[i]?.ToString() ?? "[red]NULL[/]"); // Mark NULL values with red
                        }
                        table.AddRow(row.ToArray());
                    }

                    // Add the table to the console with scrollable functionality
                    AnsiConsole.Write(
                        new Panel(table)
                            .Expand()  // Make the panel expand to fit the content
                            .BorderColor(Color.Cyan1)  // Change the panel border color
                            .Header("[bold cyan]Table Data[/]")  // Add header to the panel
                            .HeaderAlignment(Justify.Center)
                    );
                }

                AnsiConsole.MarkupLine("[yellow]Press [bold]Enter[/] to continue...[/]");
                Console.ReadLine();
                Console.Clear();
                LogoHandler.DisplayHeader(_sqlConnection.Database);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }


        // Filter Data
        private async Task FilterData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to filter data:[/]")
                    .AddChoices(tableNames));

            var columns = new List<string>();
            using (SqlCommand command = new SqlCommand($"SELECT * FROM {tableName}", _sqlConnection))
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }
            }

            var filters = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[bold yellow]What do you want to filter by[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a filter, " +
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(columns));

            if (filters.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No filters selected. Please try again.[/]");
                return;
            }

            var conditions = new List<string>();
            foreach (var filter in filters)
            {
                Console.WriteLine($"Enter the condition for {filter} (e.g., = 'value'):");
                string? condition = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(condition))
                {
                    conditions.Add($"{filter} {condition}");
                }
            }

            if (conditions.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No valid conditions entered. Please try again.[/]");
                return;
            }

            string query = $"SELECT * FROM {tableName} WHERE {string.Join(" AND ", conditions)}";

            AnsiConsole.MarkupLine("[yellow]The following SQL will be executed:[/]");
            AnsiConsole.Write(new Panel(query).BorderColor(Color.Green));

            if (!AnsiConsole.Confirm("[cyan]Do you want to execute this SQL?[/]"))
            {
                AnsiConsole.MarkupLine("[red]Table creation canceled.[/]");
                await Task.Delay(1500);
                Console.Clear();
                LogoHandler.DisplayHeader(_sqlConnection.Database);
                return;
            }


            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table()
                        .BorderColor(Color.Green);  // Set border color for the table

                    // Add table headers based on database columns
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        table.AddColumn(reader.GetName(i));
                    }

                    // Add table rows from the data
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader[i]?.ToString() ?? "[red]NULL[/]"); // Mark NULL values with red
                        }
                        table.AddRow(row.ToArray());
                    }

                    // Add the table to the console with scrollable functionality

                    Console.WriteLine(); // Readability

                    AnsiConsole.Write(
                        new Panel(table)
                            .Expand()  // Make the panel expand to fit the content
                            .BorderColor(Color.Cyan1)  // Change the panel border color
                            .Header("[bold cyan]Filtered Data[/]")  // Add header to the panel
                            .HeaderAlignment(Justify.Center)
                    );
                }

                AnsiConsole.MarkupLine("[yellow]Press [bold]Enter[/] to continue...[/]");
                Console.ReadLine();
                Console.Clear();
                LogoHandler.DisplayHeader(_sqlConnection.Database);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }

        // Sort Data
        private async Task SortData()
        {
            var tableNames = await GetAllTableNames();

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to sort data:[/]")
                    .AddChoices(tableNames));

            var columns = new List<string>();
            using (SqlCommand command = new SqlCommand($"SELECT * FROM {tableName}", _sqlConnection))
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }
            }

            if (columns.Count() == 0)
            {
                AnsiConsole.MarkupLine("[red]No columns found in the table.[/]");
                return;
            }

            string columnName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a column to sort by:[/]")
                    .AddChoices(columns));

            string sortOrder = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Ascending or descending?:[/]")
                    .AddChoices("ASC", "DESC"));

            string query = $"SELECT * FROM {tableName} ORDER BY {columnName} {sortOrder.ToUpper()}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table()
                                .BorderColor(Color.Green);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        table.AddColumn(reader.GetName(i));
                    }

                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader[i]?.ToString() ?? "NULL");
                        }
                        table.AddRow(row.ToArray());
                    }

                    Console.WriteLine(); // Readability

                    AnsiConsole.Write(
                        new Panel(table)
                            .Expand()  // Make the panel expand to fit the content
                            .BorderColor(Color.Cyan1)  // Change the panel border color
                            .Header("[bold cyan]Sorted Data[/]")  // Add header to the panel
                            .HeaderAlignment(Justify.Center)
                    );

                    AnsiConsole.MarkupLine("[yellow]Press [bold]Enter[/] to continue...[/]");
                    Console.ReadLine();
                    Console.Clear();
                    LogoHandler.DisplayHeader(_sqlConnection.Database);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }
    }
}