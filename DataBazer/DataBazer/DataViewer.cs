using Microsoft.Data.SqlClient;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            while (true)
            {
                // Display the menu without clearing the screen at the start
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Data Viewing[/]")
                        .AddChoices("View Tables", "Filter Data", "Sort Data", "Custom SQL", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                // Clear the screen after the prompt if necessary
                Console.Clear();

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

                    case "Custom SQL":
                        await CustomSQL();
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
                    var table = new Table();
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

                    AnsiConsole.Write(table);
                }
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

            Console.WriteLine("Enter the filter condition (e.g., column = 'value'):");
            string? condition = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(condition))
            {
                AnsiConsole.MarkupLine("[red]Filter condition cannot be empty. Please try again.[/]");
                return;
            }

            string query = $"SELECT * FROM {tableName} WHERE {condition}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table();
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

                    AnsiConsole.Write(table);
                }
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

            Console.WriteLine("Enter the column name to sort by:");
            string? columnName = Console.ReadLine();

            Console.WriteLine("Enter the sort order (ASC/DESC):");
            string? sortOrder = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(sortOrder))
            {
                AnsiConsole.MarkupLine("[red]Column name and sort order cannot be empty. Please try again.[/]");
                return;
            }

            string query = $"SELECT * FROM {tableName} ORDER BY {columnName} {sortOrder.ToUpper()}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table();
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

                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }

        // Custom SQL Query
        private async Task CustomSQL()
        {
            Console.WriteLine("Enter your custom SQL query:");
            string? query = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(query))
            {
                AnsiConsole.MarkupLine("[red]Query cannot be empty. Please try again.[/]");
                return;
            }

            try
            {
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    var table = new Table();
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

                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }
    }
}