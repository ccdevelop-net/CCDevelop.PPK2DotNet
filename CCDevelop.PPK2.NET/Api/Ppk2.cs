using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CCDevelop.SerialPort;

namespace CCDevelop.PPK2.NET.Api {
  public class Ppk2 : IDisposable {
    
    private SerialPortEx _serialPpk2;
    
    private Dictionary<string, dynamic> _modifiers = new Dictionary<string, dynamic>() {
                                                       { "Calibrated", null },
                                                       { "R", new[] { 1031.64, 101.65, 10.15, 0.94, 0.043 } },
                                                       { "GS", new[] { 1, 1, 1, 1, 1 } },
                                                       { "GI", new[] { 1, 1, 1, 1, 1 } },
                                                       { "O", new[] { 0, 0, 0, 0, 0 } },
                                                       { "S", new[] { 0, 0, 0, 0, 0 } },
                                                       { "I", new[] { 0, 0, 0, 0, 0 } },
                                                       { "UG", new[] { 1, 1, 1, 1, 1 } },
                                                       { "HW", null },
                                                       { "IA", null },
                                                     };    
    
    private uint   _vddLow     = 800;
    private uint   _vddHigh    = 5000;
    private uint   _currentVdd = 0;
    private double _adcMult    = 1.8 / 163840;
    private string _mode       = string.Empty;
    //self.MEAS_ADC = self._generate_mask(14, 0)
    //self.MEAS_RANGE = self._generate_mask(3, 14) 
    //self.MEAS_LOGIC = self._generate_mask(8, 24)
    //self.rolling_avg = None
    //self.rolling_avg4 = None
    //self.prev_range = None
    private uint   _consecutiveRangeSamples = 0;
    private double _spikeFilterAlpha        = 0.18;
    private double _spikeFilterAlpha5       = 0.06;
    private uint   _spikeFilterSamples      = 3;
    private uint   _afterSpike              = 0;

    private List<byte> _receivedData = new List<byte>();

    private object _lock = new object();
      
    public Ppk2(string serialName) {
      _serialPpk2 = new SerialPortEx();
      _serialPpk2.SetPortInfo(serialName, 9600);
      _serialPpk2.Connect();
      
      _serialPpk2.StatusConnectionChanged += SerialPpk2OnStatusConnectionChanged;
      _serialPpk2.DataReceived += SerialPpk2OnDataReceived;
    }

    #region PRIVATE - Serial Port Event
    //------------------------------------------------------------------------------------------------------------------
    private void SerialPpk2OnDataReceived(object sender, DataReceivedEventArgs args) {
      lock (_lock) {
        _receivedData.AddRange(args.Data);
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    private void SerialPpk2OnStatusConnectionChanged(object sender, StatusConnectionChangedEventArgs args) {
      
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PUBLIC - Static Functions
    //------------------------------------------------------------------------------------------------------------------
    public static string[] ListDevices() {
      SerialPortInfo[] serials = SerialPortEx.Ports();

      if (serials != null) {
        foreach (SerialPortInfo desc in serials) {
          if (desc.Description.Contains("PPK2")) {
            return new[] { desc.Name };
          }
        }
      }

      return null;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    #region PUBLIC - Implement IDisposable 
    //------------------------------------------------------------------------------------------------------------------
    public void Dispose() {
      if (_serialPpk2 != null) {
        WriteSerial(Ppk2Command.Reset);
          
        _serialPpk2.Disconnect();
      }
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion
    
    #region PUBLIC - Functions
    //------------------------------------------------------------------------------------------------------------------
    public bool GetModifiers() {
      /* Gets and sets modifiers from device memory */
      WriteSerial(Ppk2Command.GetMetaData);
      string metadata = ReadMetadata();
      return ParseMetadata(metadata);
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion
    
    
    #region PRIVATE - Functions
    private string ReadMetadata() {
      string read = string.Empty;
      
      /* Read metadata */
      // try to get metadata from device
      for (int i = 0; i < 5; i++) {
        Thread.Sleep(100);

        lock (_lock) {
          read += Encoding.ASCII.GetString(_receivedData.ToArray(), 0, _receivedData.Count);
        }

        // it appears the second reading is the metadata
        //read =  ser.Read(ser.BytesToRead);
        
        // TODO add a read_until serial read function with a timeout
        if (!string.IsNullOrEmpty(read) && read.Contains("END")) {
          return read;
        }
      }
      
      return string.Empty;
    }

    private bool ParseMetadata(string metadata) {
      /* Parse metadata and store it to modifiers */
      // TODO handle more robustly
      try {
        IEnumerable<string[]> dataSplit = metadata.Split('\n').Select(row => row.Split(':'));

        foreach (string key in _modifiers.Keys) {
          foreach (string[] dataPair in dataSplit) {
            if (key == dataPair[0]) {
              _modifiers[key] = dataPair[1];
            }
            for (int ind = 0; ind < 5; ind++) {
              if (key + ind == dataPair[0]) {
                if (dataPair[0].Contains("R")) {
                  // problem on some PPK2s with wrong calibration values - this doesn't fix it
                  if (float.Parse(dataPair[1]) != 0) {
                    _modifiers[key][ind.ToString()] = float.Parse(dataPair[1]);
                  }
                } else {
                  _modifiers[key][ind.ToString()] = float.Parse(dataPair[1]);
                }
              }
            }
          }
        }
        return true;
      } catch (Exception e) {
        // if exception triggers serial port is probably not correct
        return false;
      }
    }    
    //------------------------------------------------------------------------------------------------------------------
    private byte[] PackStruct(Ppk2Command command, byte[] cmdData = null) {
      byte[] data = new byte[1 + ((cmdData != null) ? cmdData.Length : 0)];

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
        _serialPpk2.SendData(cmdPacked);
      } catch (Exception e) {
        Console.WriteLine("An error occured when writing to serial port: " + e.Message);
        return false;
      }

      return true;
    }
    //------------------------------------------------------------------------------------------------------------------
    private Dictionary<string, int> GenerateMask(int bits, int pos) {
      int mask = ((1 << bits) - 1) << pos;
      mask = TwosComp(mask);
      return new Dictionary<string, int> { { "mask", mask }, { "pos", pos } };
    }
    //------------------------------------------------------------------------------------------------------------------
    private int TwosComp(int val) {
      if ((val & (1 << (32 - 1))) != 0) {
        val = val - (1 << 32);
      }
      return val;
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion
    
  }
}