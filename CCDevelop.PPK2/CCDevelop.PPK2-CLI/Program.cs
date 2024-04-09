using System.Reflection;
using System.Runtime.InteropServices;
using CCDevelop.PPK2.NET;

namespace CCDevelop.PPK2CLI; // Note: actual namespace depends on the project name.

internal class Program {
  
  #region PRIVATE - Static Variables

  private static CCDevelop.PPK2.NET.PowerProfile? _ppk2 = null;
  #endregion
  
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

    _ppk2 = new PowerProfile();
    if (!_ppk2.Init()) {
      Environment.Exit(-1);
    }
    
    Console.WriteLine("Hello World!");
  }

  private static void CurrentDomainOnProcessExit(object? sender, EventArgs e) {
    // ReSharper disable once RedundantCheckBeforeAssignment
    if (_ppk2 != null) {
      _ppk2 = null;
    } 
  }

  #region PRIVATE - Static Variables
  // Flag if set true application is running on Linux OS, otherwise false is Windows
  private static bool _onLinux = true;
  #endregion

}