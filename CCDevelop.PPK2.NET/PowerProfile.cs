using CCDevelop.SerialPort;

namespace CCDevelop.PPK2.NET {
  public class PowerProfile {
    public PowerProfile(string serialName = "", uint voltage = 3300, string filename = "") {
      _serialName = serialName;
      _voltage    = voltage;
      _filename   = filename;
    }
    
    #region PRIVATE - Variables

    private Api.Ppk2 _ppk2       = null;
    private string   _serialName = string.Empty; // Name of the serial port
    private uint     _voltage    = 0;            // Output Voltage
    private string   _filename   = string.Empty; // Name of the serial port
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
      
      
      if (_ppk2.GetModifiers()) {
        
      } 
      /*print(f"Initialized ppk2 api: {ret}")
      except Exception as e:
      print(f"Error initializing power profiler: {e}")
      ret = None
      raise e*/

      return true;
    }
    //------------------------------------------------------------------------------------------------------------------
    public void Terminate() {
      _ppk2.Dispose();
    }
    //------------------------------------------------------------------------------------------------------------------
    #endregion

    private string DiscoverPort() {
      /*"""Discovers ppk2 serial port"""
      ppk2s_connected = PPK2_API.list_devices()
      if (len(ppk2s_connected) == 1):
      ppk2_port = ppk2s_connected[0]
      print(f'Found PPK2 at {ppk2_port}')
      return ppk2_port
      else:
      print(f'Too many connected PPK2\'s: {ppk2s_connected}')*/
      return string.Empty;
    }
  }
}