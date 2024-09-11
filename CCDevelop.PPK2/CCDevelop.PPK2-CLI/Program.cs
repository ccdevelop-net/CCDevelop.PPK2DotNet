// CCDevelop - Power Profiler Kit II - Command Line Interface
// Copyright (C) 2024 - Cristian Croci
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Reflection;
using System.Runtime.InteropServices;
using CCDevelop.CommandParser;
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
    Console.Title = $"CCDevelop PPK2 CLI V{Assembly.GetExecutingAssembly().GetName().Version}";
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"Power Profiler Kit II - CLI V{Assembly.GetExecutingAssembly().GetName().Version} - by Cristian Croci [CCDevelop]");
    Console.WriteLine($"Running on: {(_onLinux ? "Linux" : "Windows")}");
    Console.WriteLine();
    
    // Register commands
    CommandLineProcessor processor = new CommandLineProcessor(new ConsoleHost());
    processor.RegisterCommand<TestPowerProfile>("testpp");
    processor.RegisterCommand<TestPpk2>("testppk2");
    
    // Process command
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