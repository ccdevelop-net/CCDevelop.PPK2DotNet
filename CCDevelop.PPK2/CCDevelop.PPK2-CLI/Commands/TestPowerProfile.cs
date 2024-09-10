using System.ComponentModel;
using CCDevelop.CommandParser;
using CCDevelop.CommandParser.Interfaces;
using CCDevelop.PPK2.NET;

namespace CCDevelop.PPK2CLI.Commands;

[Description("Execute a test with Power Profile Class.")]
public class TestPowerProfile : IConsoleCommand {
  #region PRIVATE - Static Variables
  // Power Profile Manager
  private static PowerProfile? _powerProfile2 = null;
  #endregion

  private void Terminate() {
    if (_powerProfile2 != null) {
      _powerProfile2.Terminate();
      _powerProfile2 = null;
    }     
  }

  public async Task<object?> RunAsync(CommandLineProcessor processor, IConsoleHost host) {
    
    _powerProfile2 = new PowerProfile();
    if (!_powerProfile2.Init()) {
      Terminate();
      return Task.FromResult<object?>(false);
    }

    Terminate();
    return Task.FromResult<object?>(null);
  }

}