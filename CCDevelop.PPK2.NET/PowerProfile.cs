using System.Diagnostics;
using CCDevelop.PPK2.NET.Api;

namespace CCDevelop.PPK2.NET {
  public class PowerProfile {
    public PowerProfile(string serialName = "", uint voltage = 3300, string filename = "") {
      _serialName = serialName;
      _voltage    = voltage;
      _filename   = filename;
    }
    
    #region PRIVATE - Variables
    private Api.Ppk2 _ppk2       = null;
    private string   _serialName;   // Name of the serial port
    private uint     _voltage  = 0; // Output Voltage
    private string   _filename;     // Name of the serial port
    private string   _error;
    #endregion

    #region PUBLIC - Functions
    //------------------------------------------------------------------------------------------------------------------
    public bool Init() {
      if (_serialName == null || _serialName == "") {
        string[] devices = Api.Ppk2.ListDevices();

        if (devices.Length != 1) {
          return false;
        } 
        _serialName = devices[0];
        _ppk2       = new Api.Ppk2(_serialName);
      } else {
        _ppk2 = new Api.Ppk2(_serialName);
      }
      
      // Read Modifiers
      if (_ppk2.GetModifiers()) {
        if (_ppk2.UseSourceMeter()) {
          if (_ppk2.SetSourceVoltage(_voltage)) {
            Debug.WriteLine($"Set power profiler source voltage: {_voltage}");
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
      _ppk2.Dispose();
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

#endregion
  }
}