// ============================================================================
// This file is part of PPKII-NET (https://github.com/cristianc1972/ppk2-net)
// Copyright (C) 2024 Cristian Croci
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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ===========================================================================
// File        : SerialPortExEventsArgs.cs
// Author      : Cristian Croci
// Date        : Thu, 29 Feb 2024 14:09:18 +0100
// Description : Serial Port Extended version events
// Notes       : <none>
// ===========================================================================
// Change History :
//     2024-02-29 - First Version
// ===========================================================================
namespace PpkII.NET.SerialPort {
  /**
   * Connected state changed event arguments.
   */
  public class StatusConnectionChangedEventArgs {
    #region PUBLIC - Properties
    public bool Connected { get; private set; } // Connected state of the serial port.
    #endregion

    // Create new instance of the StatusConnectionChangedEventArgs class.
    public StatusConnectionChangedEventArgs(bool state) {
      Connected = state;
    }
  }
  
  /**
   * Data received event arguments.
   */
  public class DataReceivedEventArgs {
    #region PUBLIC - Properties
    public byte[] Data { get; private set; }  // Data received
    #endregion

    // Create new instance of the DataReceivedEventArgs class.
    public DataReceivedEventArgs(byte[] data) {
      Data = data;
    }
  }
  
  /**
   * Data received event arguments.
   */
  public class SerialErrorEventArgs {
    #region PUBLIC - Properties
    public string ErrorDescription { get; private set; } // Data received
    #endregion

    // Create new instance of the DataReceivedEventArgs class.
    public SerialErrorEventArgs(string errorDescription) {
      ErrorDescription = errorDescription;
    }
  }
}