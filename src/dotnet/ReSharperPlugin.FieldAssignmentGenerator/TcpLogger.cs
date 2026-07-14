using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ReSharperPlugin.FieldAssignmentGenerator;

public class TcpLogger : IDisposable {
  private const string HOST = "127.0.0.1";
  private const int PORT = 9999;

  private static TcpLogger m_instance; 

  private TcpClient m_client;
  private StreamWriter m_writer;
  
  public static TcpLogger Get() {
    if (m_instance is null) {
      m_instance = new TcpLogger();
    }

    return m_instance;
  }

  public static void SLog(string message) {
    Get().Log(message);
  }
  
  public static void SLogType(object type) {
    Get().Log($"{type}: ${type?.GetType().FullName}");
  }
  
  public static void SLogType(string prefix, object type) {
    Get().Log($"{prefix} - {type}: ${type?.GetType().FullName}");
  }
  
  public static void SLogInterfaces(object type) {
    Get().Log($"Interfaces for {type}: ${type?.GetType().FullName}:");
    foreach (var i in type?.GetType()?.GetInterfaces()) {
      Get().Log($"- {i.FullName}");
    }
  }

  public void Log(string message) {
    try {
      EnsureConnected();
      m_writer?.WriteLine(message);
    }
    catch (Exception ex) {
      System.Diagnostics.Debug.WriteLine($"[Logger Offline]: {ex.Message}");
      System.Diagnostics.Debug.WriteLine($"[Missed Log]: {message}");
      Disconnect();
    }
  }

  private void EnsureConnected() {
    if (m_client is { Connected: true }) {
      return;
    }

    Disconnect();

    m_client = new TcpClient();
    m_client.Connect(HOST, PORT);

    var stream = m_client.GetStream();
    var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    m_writer = new StreamWriter(stream, utf8WithoutBom) { AutoFlush = true };
  }

  private void Disconnect() {
    m_writer?.Dispose();
    m_client?.Dispose();
    m_writer = null;
    m_client = null;
  }

  public void Dispose() {
    Disconnect();
  }
}