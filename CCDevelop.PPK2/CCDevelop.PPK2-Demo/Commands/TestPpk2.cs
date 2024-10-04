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
using CCDevelop.PPK2.NET.Api;

namespace CCDevelop.PPK2CLI.Commands;

[Description("Execute a test with PPK2 API Class.")]
public class TestPpk2 : ITerminalCommand {
  
  #region PRIVATE - Static Variables
  private Ppk2?  _ppk2;                      // PPK2 Api
  private string _serialName = string.Empty; // Serial name
  #endregion

  public async Task<object?> RunAsync(TerminalLineProcessor processor, ITerminalHost host) {
    string[] devices = Ppk2.ListDevices();

    if (devices.Length <= 0) {
      return Task.FromResult<object?>(false);
    } 
    _serialName = devices[0];
    _ppk2       = new Ppk2(_serialName);
    
    Print(host, "Read Modifiers : ");
    if (!_ppk2.GetModifiers()) {
      PrintError(host);      
      return Task.FromResult<object?>(false);
    }
    PrintSuccess(host);
      
    Print(host, "Set source voltage at 3.3V : ");
    if (!_ppk2.SetSourceVoltage(3300)) {
      PrintError(host);
      return Task.FromResult<object?>(false);
    }
    PrintSuccess(host);
    
    // Set source meter mode
    Print(host, "Set source meter : ");
    if (!_ppk2.UseSourceMeter()) {
      PrintError(host);
      return Task.FromResult<object?>(false);
    }  
    PrintSuccess(host);
    
    // Enable DUT power
    Print(host, "Power ON DUT : ");
    if (!_ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.On)) {
      PrintError(host);
      return Task.FromResult<object?>(false);
    }
    PrintSuccess(host);

    // Start Measuring
    Print(host, "Start measuring : ");
    if (_ppk2.StartMeasuring()) {
      PrintSuccess(host);
      Print(host, "");

      _ = ExecuteMeasure(host);
    }

    // Disable DUT power
    Print(host, "Power OFF DUT : ");
    if (!_ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.Off)) {
      PrintError(host);
      return Task.FromResult<object?>(false);
    }
    PrintSuccess(host);

    // Set ampere meter mode
    Print(host, "Set ampere meter : ");
    if (!_ppk2.UseAmpereMeter()) {
      PrintError(host);
      return Task.FromResult<object?>(false);
    }  
    PrintSuccess(host);

    // Start Measuring
    Print(host, "Start measuring : ");
    if (_ppk2.StartMeasuring()) {
      PrintSuccess(host);
      Print(host, "");

      _ = ExecuteMeasure(host);
    }

    // Valid PPK2 class
    if (_ppk2 != null) {
      _ppk2.Dispose();
      _ppk2 = null;
    }
    
    return Task.FromResult<object?>(null);
  }

  private bool ExecuteMeasure(ITerminalHost host) {
    try {
      if (_ppk2 == null) {
        return false;
      }
      
      // Measurements are a constant stream of bytes
      // the number of measurements in one sampling period depends on the wait between serial reads
      // it appears the maximum number of bytes received is 1024
      // the sampling rate of the PPK2 is 100 samples per millisecond
      for (int sample = 0; sample < 1000; sample++) {

        byte[]?          data             = null;
        List<double>?    samples          = null;
        List<int>?       rawDigitalOutput = null;
        List<List<int>>? digitalChannels  = null;

        // Wait for minimum 1024 bytes on buffer
        while (_ppk2.GetDataLength() < 1024) {
          Thread.Sleep(10);
        }
        
        // Read data
        data = _ppk2.GetData();
        
        // Check valid data
        if (data != null) {
          _ppk2.GetSamples(data, out samples, out rawDigitalOutput);
          Console.WriteLine($"Average of {samples.Count} samples is: {samples.Sum() / samples.Count}uA");

          // Raw digital contains the raw digital data from the PPK2
          // The number of raw samples is equal to the number of samples in the samples list
          // We have to process the raw digital data to get the actual digital data
          digitalChannels = _ppk2.DigitalChannels(rawDigitalOutput);

          // Loop all channels
          int channel = 1;
          foreach (List<int> ch in digitalChannels) {
            Print(host, $"Digital CH {channel} : ");
            // Print last 10 values of each channel
            for (int bit = 0; bit < 10; bit++) {
              Print(host,$"{ch[bit]}");
            }
            
            Print(host, Environment.NewLine);

            channel++;
          }

          Console.WriteLine();
        }

        Print(host, $"PPK2 Test Terminated.{Environment.NewLine}");
        
        Thread.Sleep(10);
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
      return false;
    }

    return true;
  }
  
  private void Print(ITerminalHost host, string toPrint) {
    host.Write(toPrint);
  }

  private void PrintError(ITerminalHost host) {
    host.WriteError("ERROR!!!" + Environment.NewLine);
  }

  private void PrintSuccess(ITerminalHost host) {
    host.WriteLine("SUCCESS" + Environment.NewLine);
  }

}