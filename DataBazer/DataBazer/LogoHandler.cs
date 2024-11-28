using Spectre.Console;

namespace DataBazer
{
    internal class LogoHandler
    {
        public static void DisplayLogo()
        {
            string asciiArt = @"
██████  ██████     ███████ ██████  
██   ██ ██   ██ ██ ██      ██   ██ 
██   ██ ██████     █████   ██████  
██   ██ ██   ██ ██ ██      ██   ██ 
██████  ██████     ███████ ██   ██ 
--------------------------------------------
";

            // Write the ASCII art below
            AnsiConsole.Write(
                new Markup($"[yellow]{asciiArt}[/]").Centered());
        }

        public static void DisplayHeader(string? selectedDatabase = null)
        {
            //Console.Clear(); // Clear the console for a clean look
            DisplayLogo();

            if (!string.IsNullOrWhiteSpace(selectedDatabase))
            {
                AnsiConsole.Write(
                    new Markup($"[yellow]Selected Database: {selectedDatabase}\n[/]")
                        .Centered());
            }
        }
    }
}
