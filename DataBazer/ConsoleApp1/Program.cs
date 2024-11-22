using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace DataBazer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SqlConnection sqlConnection = HandleGetDatabase();
            await MainMenu(sqlConnection);
        }

        private static async Task MainMenu(SqlConnection sqlConnection)
        {
            while (true)
            {
                Console.Clear();
                //Logo.DisplayFullLogo();
                //UserDetails();

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Main Menu[/]")
                        .AddChoices("Database Management", "Table Management", "Data Management", "Index Management", "Data Viewing")
                        .AddChoiceGroup("", "[red]Exit[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                switch (selection.Trim())
                {
                    case "Database Management":
                        Console.Clear();
                        await DatabaseManager(sqlConnection);
                        break;

                    case "Table Management":
                        Console.Clear();
                        await TableManager(sqlConnection);
                        break;

                    case "Data Management":
                        Console.Clear();
                        await EditDataManager(sqlConnection);
                        break;

                    case "Index Management":
                        Console.Clear();
                        AnsiConsole.MarkupLine("[yellow]Index Management is not implemented yet.[/]");
                        break;

                    case "Data Viewing":
                        Console.Clear();
                        await ViewDataManager(sqlConnection);
                        break;

                    case "[red]Exit[/]":
                        Console.Clear();
                        AnsiConsole.MarkupLine("[green]Exiting application. Goodbye![/]");
                        sqlConnection.Close();
                        return;

                    default:
                        Console.Clear();
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }

        private static async Task<List<string>> GetAllDatabases(SqlConnection sqlConnection)
        {
            var databases = new List<string>();
            string query = "SELECT name FROM sys.databases WHERE state = 0"; // state = 0 means the database is online.

            try
            {
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
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

        private static async Task<List<string>> GetAllTableNames(SqlConnection sqlConnection)
        {
            var tableNames = new List<string>();
            string query = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'";

            try
            {
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
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

        private static SqlConnection HandleGetDatabase()
        {
            SqlConnection? sqlConnection = null;

            while (true)
            {
                // Display a menu for user selection
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]Database Management[/]")
                        .AddChoices("Pick Database", "Create Database", "[red]Exit[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                switch (selection.Trim())
                {
                    case "Pick Database":
                        sqlConnection = SelectDatabase();
                        break;

                    case "Create Database":
                        sqlConnection = CreateNewDatabase();
                        break;

                    case "[red]Exit[/]":
                        Console.WriteLine("Goodbye!");
                        Environment.Exit(0);
                        break;

                    default:
                        Console.WriteLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }

                if (sqlConnection != null) return sqlConnection;
            }
        }

        private static SqlConnection SelectDatabase()
        {
            while (true)
            {
                // Display a message in a styled format
                AnsiConsole.MarkupLine("[yellow]Fetching available databases...[/]");

                try
                {
                    using (var serverConnection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                    {
                        serverConnection.Open();
                        var databases = GetAllDatabases(serverConnection).Result; // Fetch all databases

                        // Display the list of databases with better styling
                        AnsiConsole.MarkupLine("[green]Available databases:[/]");

                        // Create a table to display the database list
                        var table = new Table();
                        table.AddColumn(new TableColumn("No").Centered());
                        table.AddColumn(new TableColumn("Database Name").Centered());

                        for (int i = 0; i < databases.Count; i++)
                        {
                            table.AddRow((i + 1).ToString(), databases[i]);
                        }

                        AnsiConsole.Write(table); // Display the table with the list

                        // Prompt user to select a database
                        AnsiConsole.MarkupLine("[yellow]Enter the number of the database to connect to (or press Enter to use the default {IkeaAB}):[/]");

                        string? input = Console.ReadLine();

                        string selectedDatabase;
                        if (string.IsNullOrWhiteSpace(input))
                        {
                            selectedDatabase = "IkeaAB"; // Default database
                            AnsiConsole.MarkupLine("[green]Using the default database: IkeaAB[/]");
                        }
                        else if (int.TryParse(input, out int index) && index > 0 && index <= databases.Count)
                        {
                            selectedDatabase = databases[index - 1];
                            AnsiConsole.MarkupLine($"[green]You selected: {selectedDatabase}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                            continue;
                        }

                        return GetDatabase(selectedDatabase);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    AnsiConsole.MarkupLine("[yellow]Would you like to try again? (yes/no):[/]");
                    if (Console.ReadLine()?.Trim().ToLower() != "yes")
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }


        private static SqlConnection CreateNewDatabase()
        {
            Console.WriteLine("Enter the name of the new database to create:");
            string? databaseName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                Console.WriteLine("[red]Database name cannot be empty. Please try again.[/]");
                return null!;
            }

            string query = $"CREATE DATABASE [{databaseName}]";

            try
            {
                using (var connection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True"))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine($"[green]Database {databaseName} created successfully.[/]");
                    }
                }

                // Automatically connect to the new database
                return GetDatabase(databaseName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
                return null!;
            }
        }

        private static SqlConnection GetDatabase(string databaseName)
        {
            string connectionString = @$"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True";
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            Console.WriteLine($"[green]Connected to database: {databaseName}[/]");
            return connection;
        }


        // Other methods such as DatabaseManager, TableManager, etc.
        private static async Task DatabaseManager(SqlConnection sqlConnection)
        {
            while (true)
            {
                // Display the menu without clearing the screen at the start
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Database Management[/]")
                        .AddChoices("CREATE DATABASE", "ALTER DATABASE", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                // Clear the screen after the prompt if necessary
                Console.Clear();

                switch (selection.Trim())
                {
                    case "CREATE DATABASE":
                        AnsiConsole.MarkupLine("[yellow]Create Database is not implemented yet.[/]");
                        break;

                    case "ALTER DATABASE":
                        AnsiConsole.MarkupLine("[yellow]Alter Database is not implemented yet.[/]");
                        break;

                    case "[red]Back to Main Menu[/]":
                        return; // Exit back to the main menu without clearing the screen again

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }


        private static async Task TableManager(SqlConnection sqlConnection)
        {
            while (true)
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Table Management[/]")
                        .AddChoices("CREATE TABLE", "ALTER TABLE", "DROP TABLE", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                Console.Clear();

                switch (selection.Trim())
                {
                    case "CREATE TABLE":
                        AnsiConsole.MarkupLine("[yellow]Create Table is not implemented yet.[/]");
                        break;

                    case "ALTER TABLE":
                        AnsiConsole.MarkupLine("[yellow]Alter Table is not implemented yet.[/]");
                        break;

                    case "DROP TABLE":
                        AnsiConsole.MarkupLine("[yellow]Drop Table is not implemented yet.[/]");
                        break;

                    case "[red]Back to Main Menu[/]":
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }


        // Data Viewing Manager
        private static async Task ViewDataManager(SqlConnection sqlConnection)
        {
            while (true)
            {
                // Display the menu without clearing the screen at the start
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Data Viewing[/]")
                        .AddChoices("View Data", "Filter Data", "Sort Data", "Custom SQL", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                // Clear the screen after the prompt if necessary
                Console.Clear();

                switch (selection.Trim())
                {
                    case "View Data":
                        await ViewData(sqlConnection);
                        break;

                    case "Filter Data":
                        await FilterData(sqlConnection);
                        break;

                    case "Sort Data":
                        await SortData(sqlConnection);
                        break;

                    case "Custom SQL":
                        await CustomSQL(sqlConnection);
                        break;

                    case "[red]Back to Main Menu[/]":
                        return; // Exit back to the main menu without clearing the screen again

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }




        // View Data
        private static async Task ViewData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

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
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
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
        private static async Task FilterData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

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
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
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
        private static async Task SortData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

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
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
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
        private static async Task CustomSQL(SqlConnection sqlConnection)
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
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
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


        private static async Task EditDataManager(SqlConnection sqlConnection)
        {
            while (true)
            {
                // Display the menu without clearing the screen at the start
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(10)
                        .HighlightStyle(new Style(Color.Black, Color.Yellow))
                        .Title("[bold underline rgb(190,40,0)]    Data Viewing[/]")
                        .AddChoices("Insert Data", "Update Data", "Delete Data", "[red]Back to Main Menu[/]")
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]"));

                // Clear the screen after the prompt if necessary
                Console.Clear();

                switch (selection.Trim())
                {
                    case "Insert Data":
                        await InsertData(sqlConnection);
                        break;

                    case "Update Data":
                        await UpdateData(sqlConnection);
                        break;

                    case "Delete Data":
                        await DeleteData(sqlConnection);
                        break;

                    case "[red]Back to Main Menu[/]":
                        return;

                    default:
                        AnsiConsole.MarkupLine("[red]Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }

        // Insert Data
        private static async Task InsertData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to insert data into:[/]")
                    .AddChoices(tableNames));

            string columns = AnsiConsole.Ask<string>("Enter the columns (e.g., Name, Age): ");
            string values = AnsiConsole.Ask<string>("Enter the values (e.g., 'John', 30): ");
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            try
            {
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data inserted successfully into {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }

        // Update Data
        private static async Task UpdateData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to update data in:[/]")
                    .AddChoices(tableNames));

            string setClause = AnsiConsole.Ask<string>("Enter the SET clause (e.g., Name = 'John'): ");
            string condition = AnsiConsole.Ask<string>("Enter the condition (e.g., Id = 1): ");
            string query = $"UPDATE {tableName} SET {setClause} WHERE {condition}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data updated successfully in {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }

        // Delete Data
        private static async Task DeleteData(SqlConnection sqlConnection)
        {
            var tableNames = await GetAllTableNames(sqlConnection);

            if (tableNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
                return;
            }

            var tableName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a table to delete data from:[/]")
                    .AddChoices(tableNames));

            string condition = AnsiConsole.Ask<string>("Enter the condition (e.g., Id = 1): ");
            string query = $"DELETE FROM {tableName} WHERE {condition}";

            try
            {
                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                    AnsiConsole.MarkupLine($"[green]Data deleted successfully from {tableName}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }
    }
}