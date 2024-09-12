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
using System.Linq;
using System.Text;
using System.Threading;
using CCDevelop.SerialPort.Linux;
using CCDevelop.SerialPort.Windows;
using CCDevelop.SerialPort.Abstractions;

namespace CCDevelop.PPK2.NET.Api {
  public class Ppk2 : IDisposable {
    #region PRIVATE - Variables
    // Serial port driver interface
    private ISerialPort _serialPpk2; 
    // PPK2 Modifiers value
    private Dictionary<string, dynamic> _modifiers = new () {
                                                       { "Calibrated", 0 },
                                                       { "R", new[] { 1031.64, 101.65, 10.15, 0.94, 0.043 } },
                                                       { "GS", new[] { 1.0, 1.0, 1.0, 1.0, 1.0 } },
                                                       { "GI", new[] { 1.0, 1.0, 1.0, 1.0, 1.0 } },
                                                       { "O", new[] { 0.0, 0.0, 0.0, 0.0, 0.0 } },
                                                       { "S", new[] { 0.0, 0.0, 0.0, 0.0, 0.0 } },
                                                       { "I", new[] { 0.0, 0.0, 0.0, 0.0, 0.0 } },
                                                       { "UG", new[] { 1.0, 1.0, 1.0, 1.0, 1.0 } },
                                                       { "HW", 0 },
                                                       { "IA", 0 },
                                                       { "VDD", 0 },
                                                       { "mode", 0 },
                                                     };

    private uint   _vddLow  = 800;
    private uint   _vddHigh = 5000;
    private uint   _currentVdd;
    private double _adcMult = 1.8 / 163840;

    private string _mode = string.Empty;

    private Dictionary<string, int> _measAdc;   // 14 bits at position 0 
    private Dictionary<string, int> _measRange; // 3 bits at position 14 
    private Dictionary<string, int> _measLogic; // 8 bits at position 24

    private double _rollingAvg;
    private double _rollingAvg4;
    private int    _prevRange;
    private uint   _consecutiveRangeSamples;
    private double _spikeFilterAlpha   = 0.18;
    private double _spikeFilterAlpha5  = 0.06;
    private uint   _spikeFilterSamples = 3;
    private uint   _afterSpike;

    private bool       _threadRunning = true;
    private Thread     _readThread;
    private List<byte> _receivedData  = new();

    // Remainder informations
    private Dictionary<string, dynamic> _remainder = new() {
                                                       { "sequence", new byte[0] },
                                                       { "len", 0 },
                                                     };

    // Lock object for receiving data
    private object _lock = new();
    #endregion

    #region PRIVATE - Constants
    private const int ReadBufferSize = 4096;
    #endregion

    #region PUBLIC - Enumerators
    /// <summary>
    /// PPKII Power Modes
    /// </summary>
    public enum Ppk2PowerMode : byte {
      Off = 0,
      On  = 1,
    }
    #endregion

    /// <summary>
    /// Constructor for Ppk2 class
    /// </summary>
    /// <param name="serialName">Name of serial port to use</param>
    public Ppk2(string serialName) {
      // Generate masks
      _measAdc   = GenerateMask(14, 0);
      _measRange = GenerateMask(3, 14);
      _measLogic = GenerateMask(8, 24);

      // Create and open serial port
      _serialPpk2 = CreateSerialPort(serialName, 9600);
      _serialPpk2.Open();

      // Start serial port read thread
      _readThread = new Thread(SerialRead);
      _readThread.Start();
    }

    #region PUBLIC - Implement IDisposable
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Dispose function
    /// </summary>
    public void Dispose() {
      if (_serialPpk2 != null) {
        WriteSerial(Ppk2Command.Reset);

        // Terminate thread
        _threadRunning = false;
        _readThread.Join();

        _serialPpk2.Close();
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PRIVATE - Serial Thread
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Read serial port thread 
    /// </summary>
    public void SerialRead() {
      // Thread Variables
      int bytesReceived;
      byte[] readBuffer = new byte[ReadBufferSize]; // Reserve an array to store max number of bytes possible in a frame

      // Loop until user wants to stop the program.
      while (_threadRunning) {
        // Check if serial open
        if (_serialPpk2.IsOpen) {
          try {
            // Read bytes availables
            bytesReceived = _serialPpk2.BaseStream.Read(readBuffer, 0, ReadBufferSize);

            // Check if received data
            if (bytesReceived > 0) {
              //Debug.WriteLine($"Received: {bytesReceived} bytes");
              lock (_lock) {
                for (int elem = 0; elem < bytesReceived; elem++) {
                  _receivedData.Add(readBuffer[elem]);
                }
              }
            }
          } catch (TimeoutException) {
          }
        }
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PUBLIC - Static Functions
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// List PPKII device present in the system
    /// </summary>
    /// <returns>Return array of the device's serial ports</returns>
    public static string[] ListDevices() {
      // Function Variables
      SerialPortInfo[] serials = LinuxSerialPort.Ports();
      List<string>     devices = new List<string>();

      // Check if found some ports
      if (serials != null) {
        foreach (SerialPortInfo desc in serials) {
          if (desc.Description.Contains("PPK2")) {
            devices.Add(desc.Name);
          }
        }
      }

      // Execute sorting of the array
      devices.Sort();

      return devices.Count > 0 ? devices.ToArray() : null;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PUBLIC - Functions
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Read modifiers from device
    /// </summary>
    /// <returns>Return <see cref="true"/>if modifiers are succesfully read otherwise <see cref="false"/></returns>
    public bool GetModifiers() {
      // Gets modifiers from device memory
      if (WriteSerial(Ppk2Command.GetMetaData)) {
        string metadata = ReadMetadata();
        return ParseMetadata(metadata);
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Activate device in ampere mode
    /// </summary>
    /// <returns>Return <see cref="true"/>if modifiers are succesfully read otherwise <see cref="false"/></returns>
    public bool UseAmpereMeter() {
      // Configure device to use ampere meter
      _mode = Ppk2Modes.AmpereMode;
      return WriteSerial(Ppk2Command.SetPowerMode, new[] { (byte)Ppk2Command.TriggerSet }); // 17,1
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Activate device in source mode
    /// </summary>
    /// <returns>Return <see cref="true"/>if modifiers are succesfully read otherwise <see cref="false"/></returns>
    public bool UseSourceMeter() {
      // Configure device to use source meter
      _mode = Ppk2Modes.SourceMode;
      return WriteSerial(Ppk2Command.SetPowerMode, new[] { (byte)Ppk2Command.AvgNumSet });
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Set voltage to use
    /// </summary>
    /// <param name="voltage">Voltage in millivolt range from 800 to 5000</param>
    /// <returns>Return <see cref="true"/>if modifiers are succesfully read otherwise <see cref="false"/></returns>
    public bool SetSourceVoltage(uint voltage) {
      // Inits device - based on observation only REGULATOR_SET is the command.
      // The other two values correspond to the voltage level.
      // 800mV is the lowest setting - [3,32] - the values then increase linearly

      byte[] data = ConvertSourceVoltage(voltage);
      if (WriteSerial(Ppk2Command.RegulatorSet, data)) {
        _currentVdd = voltage;
        return true;
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Enable and disable power for DUT 
    /// </summary>
    /// <param name="mode">See <paramref name="Ppk2PowerMode"/> enumerator for mode</param>
    /// <returns>Return <see cref="true"/>if modifiers are succesfully read otherwise <see cref="false"/></returns>
    public bool ToggleDutPower(Ppk2PowerMode mode) {
      // Toggle DUT power based on parameter
      if (mode == Ppk2PowerMode.On) {
        return WriteSerial(Ppk2Command.DeviceRunningSet, new[] { (byte)Ppk2Command.TriggerSet }); // 12,1
      }

      if (mode == Ppk2PowerMode.Off) {
        return WriteSerial(Ppk2Command.DeviceRunningSet, new[] { (byte)Ppk2Command.NoOp }); // 12,0
      }

      return false;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Start continuous measurement
    /// </summary>
    /// <returns>Return true if mesuring is started</returns>
    /// <returns>Return <see cref="true"/> if mesuring is started otherwise <see cref="false"/></returns>
    public bool StartMeasuring() {
      if (_currentVdd == 0) {
        if (_mode == Ppk2Modes.SourceMode || _mode == Ppk2Modes.AmpereMode) {
          throw new Ppk2Exception("Input voltage not set!");
        }
      }

      return WriteSerial(Ppk2Command.AverageStart);
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Stop mesurement
    /// </summary>
    /// <returns>Return <see cref="true"/> if mesuring is stopped otherwise <see cref="false"/></returns>
    public bool StopMeasuring() {
      // Stop continuous measurement
      return WriteSerial(Ppk2Command.AverageStop);
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Get raw data
    /// </summary>
    /// <returns>Array of bytes, are raw data measure</returns>
    public byte[] GetData() {
      // Function Variables
      byte[] data;

      lock (_lock) {
        data = new byte[_receivedData.Count];
        Array.Copy(_receivedData.ToArray(), data, _receivedData.Count);
        _receivedData.Clear();
      }

      return data;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Get amount of data to read
    /// </summary>
    /// <returns>Length of data in the buffer</returns>
    public int GetDataLength() {
      // Function Variables
      int retLength = 0;

      // Get Length
      lock (_lock) {
        retLength = _receivedData.Count;
      }

      return retLength;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Convert raw data in sample value for analog and digital 
    /// </summary>
    /// <param name="data">Raw data to convert</param>
    /// <param name="samples">Converted samples for analog measure</param>
    /// <param name="rawDigitalOutput">Converted digitals I/O</param>
    /// <returns>Return <see cref="true"/> if conversion success otherwise <see cref="false"/></returns>
    public bool GetSamples(byte[] data, out List<double> samples, out List<int> rawDigitalOutput) {
      // Function Variables
      const int sampleSize = 4; // one analog value is 4 bytes in size
      int       offset     = _remainder["len"];
      byte[]    firstReading;
      int       adcVal;

      // Output lists
      samples          = new List<double>();
      rawDigitalOutput = new List<int>();
      
      _remainder["sequence"] = new byte[4]; 
      Array.Copy(data, 0, _remainder["sequence"], 0, 4);
      firstReading = _remainder["sequence"];
      adcVal       = DigitalToAnalog(firstReading);
      (double?, int?) measure = HandleRawData(adcVal);

      if (measure.Item1 != null) { 
        samples.Add(measure.Item1.Value);
      }

      if (measure.Item2 != null) {
        rawDigitalOutput.Add(measure.Item2.Value);
      }

      offset = sampleSize - offset;
               
      while (offset <= data.Length - sampleSize) {
        byte[] nextVal = new byte[sampleSize];
        
        Array.Copy(data, offset, nextVal, 0, sampleSize);
        
        offset  += sampleSize;
        
        adcVal  =  DigitalToAnalog(nextVal);
        measure =  HandleRawData(adcVal);
        
        if (measure.Item1 != null) {
          samples.Add(measure.Item1.Value);
        }
        
        if (measure.Item2 != null) {
          rawDigitalOutput.Add(measure.Item2.Value);
        }
      }

      _remainder["sequence"] = new byte[data.Length - offset];
      Array.Copy(data, offset, _remainder["sequence"], 0, ((byte[])_remainder["sequence"]).Length);
      _remainder["len"]      = data.Length - offset;
      
      return true;
    }
    //------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Convert raw digital data to digital channels.
    /// </summary>
    /// <param name="bits">List og the bits to process</param>
    /// <returns>
    /// Returns a 2d matrix with 8 rows (one for each channel).
    /// Each row contains HIGH and LOW values for the selected channel.
    /// </returns>
    public List<List<int>> DigitalChannels(List<int> bits) {
      // Function Variables
      List<List<int>> digitalChannels = new List<List<int>>() {
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>(),
                                                                new List<int>()
                                                              };

      foreach (int sample in bits) {
        digitalChannels[0].Add((sample & 1)   >> 0);
        digitalChannels[1].Add((sample & 2)   >> 1);
        digitalChannels[2].Add((sample & 4)   >> 2);
        digitalChannels[3].Add((sample & 8)   >> 3);
        digitalChannels[4].Add((sample & 16)  >> 4);
        digitalChannels[5].Add((sample & 32)  >> 5);
        digitalChannels[6].Add((sample & 64)  >> 6);
        digitalChannels[7].Add((sample & 128) >> 7);
      }
      return digitalChannels;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PRIVATE - Functions
    //------------------------------------------------------------------------------------------------------------------
    private static ISerialPort CreateSerialPort(string portName, int baudRate = 115200, int dataBits = 8,
                                                SerialPort.Abstractions.Enums.StopBits stopBits =
                                                  SerialPort.Abstractions.Enums.StopBits.One,
                                                SerialPort.Abstractions.Enums.Parity parity =
                                                  SerialPort.Abstractions.Enums.Parity.None,
                                                SerialPort.Abstractions.Enums.Handshake handshake =
                                                  SerialPort.Abstractions.Enums.Handshake.None) {
      // Create a different serial port type depending on the OS platform
      if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) {
        return new LinuxSerialPort(portName) {
                                               // Set serial port parameters

                                               // Set drain to false so that stty doesn't attempt to flush the write buffer when configuring the port,
                                               // avoiding a potential hang when flow control is enabled.
                                               // Set this to null (or remove this line) if your stty version doesn't support the [-]drain option.
                                               EnableDrain = false,

                                               // Set the minimum bytes required to trigger a read to 0.
                                               // This means that the read will never block indefinitely, even if no data arrives.
                                               // Change this to a value > 0 if you want blocking behaviour.
                                               MinimumBytesToRead = 0,

                                               // Set the read timeout in ms.
                                               // 0 specifies an infinite timeout, so a read will only complete when MinimumBytesToRead have been read.
                                               // However, since MinimumBytesToRead is 0, this combination results in reads that do not block.
                                               ReadTimeout = LinuxSerialPort.InfiniteTimeout,

                                               // Set standard serial options
                                               BaudRate  = baudRate,
                                               DataBits  = dataBits,
                                               Parity    = parity,
                                               StopBits  = stopBits,
                                               Handshake = handshake
                                             };
      } else if (Environment.OSVersion.Platform == PlatformID.Win32NT      ||
                 Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                 Environment.OSVersion.Platform == PlatformID.Win32S) {
        return new WindowsSerialPort(new System.IO.Ports.SerialPort(portName)) {
                                                                                 BaudRate  = baudRate,
                                                                                 DataBits  = 8,
                                                                                 Parity    = parity,
                                                                                 StopBits  = stopBits,
                                                                                 Handshake = handshake,
                                                                                 ReadTimeout =
                                                                                   WindowsSerialPort.InfiniteTimeout
                                                                               };
      } else {
        // We don't know what platform we're on, fallback to the Windows implementation
        return new WindowsSerialPort(new System.IO.Ports.SerialPort(portName)) {
                                                                                 BaudRate  = baudRate,
                                                                                 DataBits  = dataBits,
                                                                                 Parity    = parity,
                                                                                 StopBits  = stopBits,
                                                                                 Handshake = handshake,
                                                                                 ReadTimeout =
                                                                                   WindowsSerialPort.InfiniteTimeout
                                                                               };
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    private string ReadMetadata() {
      // Function Variables
      string read = string.Empty;

      // Read metadata try to get metadata from device
      for (int i = 0; i < 5; i++) {
        // Wait 100 ms
        Thread.Sleep(100);

        // Lock buffer
        lock (_lock) {
          read += Encoding.ASCII.GetString(_receivedData.ToArray(), 0, _receivedData.Count);
          _receivedData.Clear();
        }
        
        // TODO add serial read function with a timeout
        if (!string.IsNullOrEmpty(read) && read.Contains("END")) {
          return read;
        }
      }

      return string.Empty;
    }
    //------------------------------------------------------------------------------------------------------------------
    private bool ParseMetadata(string metadata) {
      // Parse metadata and store it to modifiers
      try {
        // Function Variables
        IEnumerable<string[]> dataSplit = metadata.Split('\n').Select(row => row.Split(':'));

        // Loop all modifiers
        foreach (string key in _modifiers.Keys) {
          // ReSharper disable once PossibleMultipleEnumeration
          foreach (string[] dataPair in dataSplit) {
            // Check end of data
            if (dataPair[0] == "END") {
              break;
            }

            // Check is data with index
            bool isIndexed = char.IsNumber(dataPair[0].ToCharArray()[dataPair[0].Length - 1]);

            // No index present
            if (!isIndexed) {
              if (key == dataPair[0]) {
                _modifiers[key] = Convert.ChangeType(dataPair[1], _modifiers[key].GetType());
              }
            } else {
              // Check if key is more length of data
              if (key.Length > dataPair[0].Length) {
                continue;
              }

              // Verify if indexed data
              if (dataPair[0].Substring(0, key.Length) == key) {
                int index = Convert.ToInt32(dataPair[0].ToCharArray()[dataPair[0].Length - 1] - '0');
                _modifiers[key][index] = Convert.ChangeType(dataPair[1], _modifiers[key][index].GetType());
              }
            }
          }
        }

        return true;
      } catch (Exception e) {
        // If exception triggers serial port is probably not correct
        _ = e.ToString();
        return false;
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    private byte[] PackStruct(Ppk2Command command, byte[] cmdData = null) {
      byte[] data = new byte[1 + (cmdData != null ? cmdData.Length : 0)];

      data[0] = (byte)command;
      if (cmdData != null) {
        Array.Copy(cmdData, 0, data, 1, cmdData.Length);
      }

      return data;
    }

    //------------------------------------------------------------------------------------------------------------------
    private bool WriteSerial(Ppk2Command command, byte[] cmdData = null) {
      try {
        byte[] cmdPacked = PackStruct(command, cmdData);
        _serialPpk2.BaseStream.Write(cmdPacked, 0, cmdPacked.Length);
      } catch (Exception e) {
        Console.WriteLine("An error occured when writing to serial port: " + e.Message);
        return false;
      }

      return true;
    }
    //------------------------------------------------------------------------------------------------------------------
    private Dictionary<string, int> GenerateMask(int bits, int pos) {
      // Function Variables
      int mask = ((1 << bits) - 1) << pos;
      
      mask = TwosComp(mask);
      return new Dictionary<string, int> { { "mask", mask }, { "pos", pos } };
    }
    //------------------------------------------------------------------------------------------------------------------
    private int TwosComp(int val) {
      if ((val & (1 << (32 - 1))) != 0) {
        val = val - (1 << (32 - 1));
      }

      return val;
    }
    //------------------------------------------------------------------------------------------------------------------
    private byte[] ConvertSourceVoltage(uint voltage) {
      // Function Variables
      const uint offset = 32;
      byte[]     data   = new byte[2];
      uint       mV     = voltage;
      uint       diffToBaseline;
      uint       ratio;
      uint       remainder;

      // Minimal possible mV is 800
      if (voltage < _vddLow) {
        mV = _vddLow;
      }

      // Maximal possible mV is 5000
      if (mV > _vddHigh) {
        mV = _vddHigh;
      }

      // Get difference to baseline (the baseline is 800mV but the initial offset is 32)
      diffToBaseline = mV - _vddLow + offset;
      data[0]        = 3;
      data[1]        = 0; // is actually 32 - compensated with above offset

      // Get the number of times we have to increase the first byte of the command
      ratio     = diffToBaseline / 256;
      remainder = diffToBaseline % 256; // Get the remainder for byte 2

      data[0] += (byte)ratio;
      data[1] += (byte)remainder;

      return data;
    }
    //------------------------------------------------------------------------------------------------------------------
    private int DigitalToAnalog(byte[] adcValue) {
      return BitConverter.ToInt32(adcValue, 0);
    }
    //------------------------------------------------------------------------------------------------------------------
    private int GetMaskedValue(int value, Dictionary<string, int> meas, bool isBit = false) {
      if (isBit) {}
      return (value & meas["mask"]) >> meas["pos"];
    }
    //------------------------------------------------------------------------------------------------------------------
    private (double?, int?) HandleRawData(int adcValue) {
      // Convert raw value to analog value
      try {
        int currentMeasurementRange = Math.Min(GetMaskedValue(adcValue, _measRange), 4); // 5 is number of parameters
        int    adcResult   = GetMaskedValue(adcValue, _measAdc) * 4;
        int    bits        = GetMaskedValue(adcValue, _measLogic);
        double analogValue = GetAdcResult(currentMeasurementRange, adcResult) * Math.Pow(10, 6);
        return (analogValue, bits);
      } catch (Exception e) {
        Console.WriteLine("Measurement outside of range!");
        _ = e.ToString();
        return (null, null);
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    private double GetAdcResult(int currentRange, int adcValue) {
      double resultWithoutGain = (adcValue - _modifiers["O"][currentRange]) * (_adcMult / _modifiers["R"][currentRange]);
      double adc = _modifiers["UG"][currentRange] * (resultWithoutGain * (_modifiers["GS"][currentRange] * resultWithoutGain + _modifiers["GI"][currentRange]) + 
                                                     (_modifiers["S"][currentRange] * (_currentVdd / 1000) + _modifiers["I"][currentRange]));

      double prevRollingAvg  = _rollingAvg;
      double prevRollingAvg4 = _rollingAvg4;
      
      // Spike filtering / rolling average
      if (_rollingAvg == 0) {
        _rollingAvg = adc;
      } else {
        _rollingAvg = _spikeFilterAlpha * adc + (1 - _spikeFilterAlpha) * _rollingAvg;
      }

      if (_rollingAvg4 == 0) {
        _rollingAvg4 = adc;
      } else {
        _rollingAvg4 = _spikeFilterAlpha5 * adc + (1 - _spikeFilterAlpha5) * _rollingAvg4;
      }

      if (_prevRange == 0) {
        _prevRange = currentRange;
      }

      if (_prevRange != currentRange || _afterSpike > 0) {
        if (_prevRange != currentRange) {
          _consecutiveRangeSamples = 0;
          _afterSpike               = _spikeFilterSamples;
        } else {
          _consecutiveRangeSamples++;
        }

        if (currentRange == 4) {
          if (_consecutiveRangeSamples < 2) {
            _rollingAvg  = prevRollingAvg;
            _rollingAvg4 = prevRollingAvg4;
          }
          adc = _rollingAvg4;
        } else {
          adc = _rollingAvg;
        }

        _afterSpike--;
      }

      _prevRange = currentRange;
      
      return adc;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion
  }
}