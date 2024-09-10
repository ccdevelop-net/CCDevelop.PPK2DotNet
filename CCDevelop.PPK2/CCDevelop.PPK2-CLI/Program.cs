using System.Reflection;
using System.Runtime.InteropServices;
using CCDevelop.CommandParser;
using CCDevelop.PPK2.NET;
using CCDevelop.PPK2CLI.Commands;

namespace CCDevelop.PPK2CLI; // Note: actual namespace depends on the project name.

internal class Program {
  
  #region PRIVATE - Static Variables
  // Flag if set true application is running on Linux OS, otherwise false is Windows
  private static bool _onLinux = true;
  #endregion
  
  //--------------------------------------------------------------------------------------------------------------------
  private static void Main(string[] args) {
    // Add function event then process exit
    AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

    // Check if Linux
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      _onLinux = true;
    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      _onLinux = false;
    } else {
      Environment.Exit(-1);
    }
    
    // Video title
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"Power Profiler Kit II - CLI V{Assembly.GetExecutingAssembly().GetName().Version} - by Cristian Croci [CCDevelop]");
    Console.WriteLine($"Running on: {(_onLinux ? "Linux" : "Windows")}");
    Console.WriteLine();

    CommandLineProcessor processor = new CommandLineProcessor(new ConsoleHost());
    processor.RegisterCommand<TestPowerProfile>("testpp");
    processor.RegisterCommand<TestPpk2>("testppk2");
    IList<CommandResult> result = processor.Process(args);

    // Loop results
    foreach (CommandResult res in result) {
      
    }

    // Exit application
    Environment.Exit(0);
  }
  //--------------------------------------------------------------------------------------------------------------------

  
  #region PRIVATE - Static application events
  //--------------------------------------------------------------------------------------------------------------------
  private static void CurrentDomainOnProcessExit(object? sender, EventArgs e) {

  }
  //--------------------------------------------------------------------------------------------------------------------
  #endregion
}