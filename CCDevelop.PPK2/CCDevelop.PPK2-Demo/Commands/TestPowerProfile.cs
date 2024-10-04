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

using System.ComponentModel;
using CCDevelop.CommandParser;
using CCDevelop.CommandParser.Interfaces;
using CCDevelop.PPK2.NET;

namespace CCDevelop.PPK2CLI.Commands;

[Description("Execute a test with Power Profile Class.")]
public class TestPowerProfile : ITerminalCommand {
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

  public async Task<object?> RunAsync(TerminalLineProcessor processor, ITerminalHost host) {
    // Allocate power profile
    _powerProfile2 = new PowerProfile();
    
    // Initialize power profile 
    if (!_powerProfile2.Init()) {
      Terminate();
      return Task.FromResult<object?>(false);
    }

    // Start Measure 
    if (_powerProfile2.StartMeasuring()) {
      for (int tm = 0; tm < 100; tm++) {
        host.WriteLine($"{tm} -> Time: {(_powerProfile2.GetRunningTime_mS() / 1000):0.0000} s - Samples: {_powerProfile2.GetNumMeasurements()}");
        Thread.Sleep(1000);
      }

      _powerProfile2.StopMeasuring();
    }
    
    // Terminate power profile
    Terminate();
    
    return Task.FromResult<object?>(null);
  }

}