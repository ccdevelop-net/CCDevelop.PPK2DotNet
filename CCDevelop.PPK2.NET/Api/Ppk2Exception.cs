using System;

namespace CCDevelop.PPK2.NET.Api;

public class Ppk2Exception : Exception {
  public Ppk2Exception() {
  }

  public Ppk2Exception(string message) : base(message) {
  }

  public Ppk2Exception(string message, Exception inner) : base(message, inner) {
  }
}