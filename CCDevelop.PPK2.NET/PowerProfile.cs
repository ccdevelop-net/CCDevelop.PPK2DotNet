// CCDevelop - Power Profiler Kit II API
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CCDevelop.PPK2.NET.Api;

namespace CCDevelop.PPK2.NET {
  public class PowerProfile {
    public PowerProfile(string serialName = "", uint voltage = 3300, string filename = "") {
      _serialName      = serialName;
      _voltage         = voltage;
      _filename        = filename;
    }
    
    #region PRIVATE - Variables
    private Ppk2         _ppk2 = null;
    private string       _serialName;                          // Name of the serial port
    private uint         _voltage              = 0;            // Output Voltage
    private string       _filename             = string.Empty; // Name of the serial port
    private string       _error                = string.Empty;
    private bool         _measuring            = false;
    private List<double> _currentMeasurements  = null;
    private DateTime     _measurementStartTime = DateTime.MinValue;
    private DateTime     _measurementStopTime  = DateTime.MinValue;
    private bool         _threadRunning        = false;
    private Thread       _measureThread        = null;
    #endregion

    #region PUBLIC - Functions
    //------------------------------------------------------------------------------------------------------------------
    public bool Init() {
      if (_serialName == null || _serialName == "") {
        string[] devices = Ppk2.ListDevices();

        if (devices.Length != 1) {
          return false;
        } 
        _serialName = devices[0];
        _ppk2       = new Ppk2(_serialName);
      } else {
        _ppk2 = new Ppk2(_serialName);
      }
      
      // Read Modifiers
      if (_ppk2.GetModifiers()) {
        if (_ppk2.UseSourceMeter()) {
          if (_ppk2.SetSourceVoltage(_voltage)) {
            Debug.WriteLine($"Set power profiler source voltage: {_voltage}");
            
            // TODO: To complete
            /*
             self.measuring = False
               self.current_measurements = []

               # local variables used to calculate power consumption
               self.measurement_start_time = None
               self.measurement_stop_time = None

               time.sleep(1)

               self.stop = False

               self.measurement_thread = Thread(target=self.measurement_loop, daemon=True)
               self.measurement_thread.start()

               # write to csv
               self.filename = filename
               if self.filename is not None:
                   with open(self.filename, 'w', newline='') as file:
                       writer = csv.writer(file)
                       row = []
                       for key in ["ts", "avg1000"]:
                           row.append(key)
                       writer.writerow(row)
             */
          } else {
            return false;
          }
        } else {
          return false;
        }
      } else {
        _error = $"Error initializing power profiler: {_serialName}";
        return false;
      }

      return true;
    }
    //------------------------------------------------------------------------------------------------------------------
    public void Terminate() {
      if (_ppk2 != null) {
        _ppk2.Dispose();
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    public bool EnablePower() {
      // Enable ppk2 power
      if (_ppk2 != null) {
        return _ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.On);
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    public bool DisablePower() {
      // Disable ppk2 power
      if (_ppk2 != null) {
        return _ppk2.ToggleDutPower(Ppk2.Ppk2PowerMode.Off);
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    public bool StartMeasuring(){
      // Start measuring
      // Toggle measuring flag only if currently not measuring
      if (!_measuring && _ppk2 != null) { 
        _currentMeasurements = new List<double>(); 
        if (_ppk2.StartMeasuring()) {
          _measuring            = true;
          _measurementStartTime = DateTime.Now;
          
          // Start measuring thread
          _threadRunning = true;
          _measureThread = new Thread(MeasureThread);
          _measureThread.Start();

          return true;
        } 
        _currentMeasurements = null;
      }
      
      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    public bool StopMeasuring() {
      if (_measuring && _ppk2 != null) {
        // Stop measuring and return average of period
        _measurementStopTime = DateTime.Now;
        _measuring           = false;
        _ppk2.StopMeasuring(); // Send command to ppk2
        
        // Terminate thread
        _threadRunning = false;
        _measureThread!.Join();
        _measureThread = null;

        return true;
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetMinCurrent_mA() {
      return _currentMeasurements!.Min() / 1000.0;
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetMaxCurrent_mA() {
      return _currentMeasurements!.Max() / 1000.0;
       
    }
    //------------------------------------------------------------------------------------------------------------------
    public int GetNumMeasurements() {
      return _currentMeasurements!.Count;
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetAverageCurrent_mA() {
      // Returns average current of last measurement in mA
      if (_currentMeasurements!.Count == 0) {
        return 0;
      }

      // Measurements are in microamperes, divide by 1000
      return (_currentMeasurements.Sum() / _currentMeasurements.Count) / 1000.0; 
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetAveragePowerConsumption_mWh() {
      double averageCurrentmA = GetAverageCurrent_mA();
      double averagePowermW = (_voltage / 1000.0) * averageCurrentmA;   // Source voltage is in millivolts, so divide by 1000 to convert to watts
      double measurementDurationH = GetMeasurementDurationS() / 3600;   // Duration in seconds, divide by 3600 to get hours
      return averagePowermW * measurementDurationH;
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetAverageCharge_mC() {
      // Returns average charge in milli coulomb
      double averageCurrentmA     = GetAverageCurrent_mA();
      double measurementDurationS = GetMeasurementDurationS();  // In seconds
      return averageCurrentmA * measurementDurationS;
    }
    //------------------------------------------------------------------------------------------------------------------
    public double GetMeasurementDurationS() {
      // Returns duration of measurement in seconds
      TimeSpan measurementDurationS = _measurementStopTime - _measurementStartTime; // Duration in seconds
      return measurementDurationS.TotalSeconds;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion
    
    #region PRIVATE - Serial Thread
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Measure thread 
    /// </summary>
    public void MeasureThread() {
      // Thread Variables
      byte[]          data             = null;
      List<double>    samples          = null;
      List<int>       rawDigitalOutput = null;
      List<List<int>> digitalChannels  = null;

      // Loop until user wants to stop the program.
      while (_threadRunning) {
        // Check if measuring enebled
        if (_measuring && _ppk2 != null) {
          // Wait for minimum 1024 bytes on buffer
          while (_ppk2.GetDataLength() < 1024) {
            Thread.Sleep(10);
          }
        
          // Read data
          data = _ppk2.GetData();
        
          // Check valid data
          if (data != null) {
            _ppk2.GetSamples(data, out samples, out rawDigitalOutput);

            if (samples != null) {
              _currentMeasurements!.AddRange(samples);
            }
          }
        }
      }
    }
    //------------------------------------------------------------------------------------------------------------------
#endregion    
  }
}