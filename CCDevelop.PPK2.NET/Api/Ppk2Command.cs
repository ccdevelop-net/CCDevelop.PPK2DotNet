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

namespace CCDevelop.PPK2.NET.Api {
  public enum Ppk2Command : byte {
    NoOp = 0x00,
    TriggerSet = 0x01,
    AvgNumSet = 0x02,
    TriggerWindowSet = 0x03,
    TriggerIntervalSet = 0x04,
    TriggerSingleSet = 0x05,
    AverageStart = 0x06,
    AverageStop = 0x07,
    RangeSet = 0x08,
    LcdSet = 0x09,
    TriggerStop = 0x0a,
    DeviceRunningSet = 0x0c,
    RegulatorSet = 0x0d,
    SwitchPointDown = 0x0e,
    SwitchPointUp = 0x0f,
    TriggerExtToggle = 0x11,
    SetPowerMode = 0x11,
    ResUserSet = 0x12,
    SpikeFilteringOn = 0x15,
    SpikeFilteringOff = 0x16,
    GetMetaData = 0x19,
    Reset = 0x20,
    SetUserGains = 0x25,   
  }
}