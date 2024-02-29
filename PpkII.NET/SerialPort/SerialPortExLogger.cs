using System;
using System.IO.Ports;
using NLog;

namespace PpkII.NET.SerialPort {
  public static class Log {
    #region PRIVATE - Static Variables
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    #endregion
    
    //-------------------------------------------------------------------------
    internal static void Debug(string message) {
      _logger.Debug(message);
    }
    //-------------------------------------------------------------------------
    internal static void Error(Exception ex) {
      _logger.Error(ex, null);
    }
    //-------------------------------------------------------------------------
    internal static void Error(SerialError error) {
      _logger.Error($"SerialPort ErrorReceived: {error}");
    }
    //-------------------------------------------------------------------------
  }
}