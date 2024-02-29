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
// File        : SerialPortEx.cs
// Author      : Cristian Croci
// Date        : Thu, 29 Feb 2024 14:07:58 +0100
// Description : Serial Port Extended Manager Class, manage a serial port
//               using events
// Notes       : <none>
// ===========================================================================
// Change History :
//     2024-02-29 - First Version
// ===========================================================================
using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
// ReSharper disable InconsistentNaming

namespace PpkII.NET.SerialPort {
  public class SerialPortEx {

    // ReSharper disable once InconsistentNaming
    private const int JOIN_TIMEOUT = 5000; 
    
    #region PUBLIC - Delegates
    // Delegate for serial connection status changed event.
    public delegate void StatusConnectionChangedEventHandler(object sender, StatusConnectionChangedEventArgs args);
    // Delegate data received event.
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs args);
    #endregion

    #region PUBLIC - Events
    // Generated when connection change status.
    public event StatusConnectionChangedEventHandler StatusConnectionChanged;
    // Generated when data received.
    public event DataReceivedEventHandler DataReceived;
    #endregion
    
    #region PRIVATE - Variables
    private System.IO.Ports.SerialPort  _serial;
    private string                      _name     = "";
    private int                         _baud     = 115200;
    private StopBits                    _stopBits = StopBits.One;
    private Parity                      _parity   = Parity.None;
    private DataBits                    _dataBits = DataBits.Eight;
    
    private Thread                      _serialReader; // Serial port reader task
    private CancellationTokenSource     _serialReaderCTS = new CancellationTokenSource();
    
    private Thread                      _serialConnectionWatcher; // Serial port connection watcher
    private CancellationTokenSource     _serialConnectionWatcherCTS = new CancellationTokenSource();

    private readonly object             _serialLock = new object();
    private          bool               _disconnectRequested;
    
    private bool                        _rwError = true; // Read/Write error state variable
    #endregion

    public SerialPortEx() {
    }
    public SerialPortEx(string portName) {
      _name = portName;
    }

    #region PUBLIC - Properties
    // Indicate if serial port is connected.
    public bool IsSerialConnected => _serial != null && !_rwError && !_disconnectRequested;
    
    // Serial port reconnection delay in milliseconds
    public int ReconnectionDelay { get; set; } = 1000;
    #endregion

    #region PUBLIC - Functions
    //-------------------------------------------------------------------------
    public bool Connect() {   // Connect to the serial port.
      if (_disconnectRequested) {
        return false;
      }
      
      lock (_serialLock) {
        Disconnect();
        Open();
        _serialConnectionWatcherCTS = new CancellationTokenSource();
        _serialConnectionWatcher    = new Thread(ConnectionWatcherTask) {
                                                                          IsBackground = true
                                                                        };
        _serialConnectionWatcher.Start(_serialConnectionWatcherCTS.Token);
      }
      
      return IsSerialConnected;
    }
    //-------------------------------------------------------------------------
    public void Disconnect() {    // Disconnect serial port
      if (_disconnectRequested) {
        return;
      }
      
      _disconnectRequested = true;
      
      Close();
      
      lock (_serialLock) {
        if (_serialConnectionWatcher != null) {
          if (!_serialConnectionWatcher.Join(JOIN_TIMEOUT)) {
            _serialConnectionWatcherCTS.Cancel();
          }
          _serialConnectionWatcher = null;
        }
        
        _disconnectRequested = false;
      }
    }
    //-------------------------------------------------------------------------    
    public void SetPortInfo(string portName, int baudRate = 115200, StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight) {
      if (portName != _name || baudRate != _baud || stopBits != _stopBits || parity != _parity || dataBits != _dataBits) {
        _name = portName;
        _baud = baudRate;
        _stopBits = stopBits;
        _parity   = parity;
        _dataBits = dataBits;
        if (IsSerialConnected) {
          Connect(); // Take into account immediately the new connection parameters
        }
        Log.Debug(string.Format($"Port parameters changed (Name: {portName} - Baudrate {baudRate} - Stop Bits {stopBits} - Parity {parity} / Data Bits {dataBits})", 
                                portName, baudRate, stopBits, parity, dataBits));
      }
    }
    //-------------------------------------------------------------------------
    public bool SendData(byte[] data) {
      // Check is serial connected
      if (IsSerialConnected) {
        try {
          _serial.Write(data, 0, data.Length);
          Log.Debug($"Sent {data.Length} bytes");
          
          return true;
        } catch (Exception e) {
          Log.Error(e);
        }
      }
      
      return false;
    }
    //-------------------------------------------------------------------------
    public bool SendData(string message) {
      return SendData(Encoding.ASCII.GetBytes(message));
    }
    //-------------------------------------------------------------------------
    #endregion
    
    #region PRIVATE - Functions
    //-------------------------------------------------------------------------
    private bool Open() {
      // Function Variables
      bool success = false;
      
      lock (_serialLock) {
        // Close serial
        Close();
        
        try {
          bool tryOpen = true;
          
          // Verify if Linux system
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            tryOpen = System.IO.File.Exists(_name);
          }
          
          // Try to open serial
          if (tryOpen) {
            _serial = new System.IO.Ports.SerialPort();
            _serial.ErrorReceived += HandleErrorReceived;
            _serial.PortName = _name;
            _serial.BaudRate = _baud;
            _serial.StopBits = _stopBits;
            _serial.Parity = _parity;
            _serial.DataBits = (int)_dataBits;

            _serial.Open();
            if (_serial.IsOpen) {
              success = true;
            }
          } else {
            
          }
        } catch (Exception e) {
          Log.Error(e);
          Close();
        }
        
        // Check Serial valid
        if (_serial != null && _serial.IsOpen) {
          _rwError = false;
          
          // Start the Reader task
          _serialReaderCTS = new CancellationTokenSource();
          _serialReader    = new Thread(SerialReaderThread) {
                                                              IsBackground = true
                                                            };
          _serialReader.Start(_serialReaderCTS.Token);
          
          Log.Debug("true");
          StatusConnectionChanged?.Invoke(this, new StatusConnectionChangedEventArgs(true));
        }
      }
      return success;
    }
    //-------------------------------------------------------------------------
    private void Close() {
      lock (_serialLock) {
        // Stop the Reader task
        if (_serialReader != null) {
          if (!_serialReader.Join(JOIN_TIMEOUT)) {
            _serialReaderCTS.Cancel();
          }

          _serialReader = null;
        }
        if (_serial != null) {
          _serial.ErrorReceived -= HandleErrorReceived;
          if (_serial.IsOpen) {
            _serial.Close();
            
            Log.Debug("false");
            StatusConnectionChanged?.Invoke(this, new StatusConnectionChangedEventArgs(false));
          }
          _serial.Dispose();
          _serial = null;
        }
        _rwError = true;
      }
    }
    //-------------------------------------------------------------------------
    private void HandleErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
      Log.Error(e.EventType);
    }
    //-------------------------------------------------------------------------
    #endregion    
    
    #region PROTECTED - Events Manager
    //-------------------------------------------------------------------------
    private void SerialReaderThread(object parameters) {
      // Function Variables
      CancellationToken cancelToken = (CancellationToken)parameters;
      
      // Main loop of the thread
      while (IsSerialConnected && !cancelToken.IsCancellationRequested) {
        // Try to receive
        try {
          // Local Variables
          int dataLength = _serial.BytesToRead;
            
          if (dataLength > 0) {
            byte[] message = new byte[dataLength];
            
            int readBytes = 0;
            while (readBytes <= 0) {
              readBytes = _serial.Read(message, readBytes, dataLength - readBytes);
            }

            // Invoke event
            DataReceived?.Invoke(this, new DataReceivedEventArgs(message));
          } else { 
            Thread.Sleep(100);
          }
        } catch (Exception e) {
          Log.Error(e);
          _rwError = true;
          Thread.Sleep(1000);
        }
      }
    }
    //-------------------------------------------------------------------------
    private void ConnectionWatcherTask(object parameters) {
      // Function Variables
      CancellationToken cancelToken = (CancellationToken) parameters;
      
      // Task automatically reconnecting the interface when the connection
      // is drop for an I/O error occurs
      while (!_disconnectRequested && !cancelToken.IsCancellationRequested) {
        // Check if in error 
        if (_rwError) {
          try {
            Close();
            
            // Wait for reconnect
            Thread.Sleep(ReconnectionDelay);
            
            // No disconnect is required 
            if (!_disconnectRequested) {
              try {
                // Open serial port
                if (!Open()) { 
                  Log.Debug("Unable to Open!!!");
                }
              } catch (Exception e) { 
                Log.Error(e);
              }
            }
          } catch (Exception e) {
            Log.Error(e);
          }
        }
        
        // No disconnect is required
        if (!_disconnectRequested) {
          Thread.Sleep(1000);
        }
      }
    }
    #endregion




  }
}