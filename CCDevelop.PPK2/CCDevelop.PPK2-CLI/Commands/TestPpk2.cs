using System.ComponentModel;
using CCDevelop.CommandParser;
using CCDevelop.CommandParser.Interfaces;
using CCDevelop.PPK2.NET.Api;

namespace CCDevelop.PPK2CLI.Commands;

[Description("Execute a test with PPK2 API Class.")]
public class TestPpk2 : IConsoleCommand {
  
  #region PRIVATE - Static Variables
  private Ppk2?  _ppk2;                      // PPK2 Api
  private string _serialName = string.Empty; // Serial name
  #endregion

  public async Task<object?> RunAsync(CommandLineProcessor processor, IConsoleHost host) {
    string[] devices = Ppk2.ListDevices();

    if (devices.Length <= 0) {
      return Task.FromResult<object?>(false);
    } 
    _serialName = devices[0];
    _ppk2       = new Ppk2(_serialName);
    
    Print("Read Modifiers : ");
    if (!_ppk2.GetModifiers()) {
      PrintError();      
      return Task.FromResult<object?>(false);
    }
    PrintSuccess();
      
    Print("Set source voltage at 3.3V : ");
    if (!_ppk2.SetSourceVoltage(3300)) {
      PrintError();
      return Task.FromResult<object?>(false);
    }
    PrintSuccess();
    
    // Set source meter mode
    Print("Set source meter : ");
    if (!_ppk2.UseSourceMeter()) {
      PrintError();
      return Task.FromResult<object?>(false);
    }  
    PrintSuccess();
    
    // Enable DUT power
    Print("Power ON DUT : ");
    if (!_ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.On)) {
      PrintError();
      return Task.FromResult<object?>(false);
    }
    PrintSuccess();

    // Start Measuring
    Print("Start measuring : ");
    if (_ppk2.StartMeasuring()) {
      PrintSuccess();
      Print("");

      _ = ExecuteMeasure();
    }

    // Disable DUT power
    Print("Power OFF DUT : ");
    if (!_ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.Off)) {
      PrintError();
      return Task.FromResult<object?>(false);
    }
    PrintSuccess();

    // Set ampere meter mode
    Print("Set ampere meter : ");
    if (!_ppk2.UseAmpereMeter()) {
      PrintError();
      return Task.FromResult<object?>(false);
    }  
    PrintSuccess();

    // Start Measuring
    Print("Start measuring : ");
    if (_ppk2.StartMeasuring()) {
      PrintSuccess();
      Print("");

      _ = ExecuteMeasure();
    }

    // Valid PPK2 class
    if (_ppk2 != null) {
      _ppk2.Dispose();
      _ppk2 = null;
    }
    
    return Task.FromResult<object?>(null);
  }

  private bool ExecuteMeasure() {
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
            Print($"Digital CH {channel} : ");
            // Print last 10 values of each channel
            for (int bit = 0; bit < 10; bit++) {
              Print($"{ch[bit]}");
            }
            
            Print(Environment.NewLine);

            channel++;
          }

          Console.WriteLine();
        }

        Print($"PPK2 Test Terminated.{Environment.NewLine}");
        
        Thread.Sleep(10);
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
      return false;
    }

    return true;
  }
  
  private void Print(string toPrint) {
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(toPrint);
  }

  private void PrintError() {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR!!!");
    Console.ForegroundColor = ConsoleColor.White;
  }

  private void PrintSuccess() {
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("SUCCESS");
    Console.ForegroundColor = ConsoleColor.White;
  }

}