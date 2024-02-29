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
// File        : SerialPortExLogger.cs
// Author      : Cristian Croci
// Date        : Thu, 29 Feb 2024 18:08:01 +0100
// Description : Manage logging of the serial
// Notes       :
// ===========================================================================
// Change History :
//     2024-02-29 - First Version
// ===========================================================================
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