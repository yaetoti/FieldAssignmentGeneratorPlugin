using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ReSharperPlugin.FieldAssignmentGenerator;

public static class AsyncTcpLogger
{
  private const string HOST = "127.0.0.1";
  private const int PORT = 9999;

  public static void Log(string message) {
    try {
      using var client = new TcpClient();
      client.Connect(HOST, PORT);
      
      using var stream = client.GetStream();
      byte[] data = Encoding.UTF8.GetBytes(message + "\n");
      
      stream.Write(data, 0, data.Length);
    }
    catch (Exception ex) {
      System.Diagnostics.Debug.WriteLine($"[TCP Log Failed]: {ex.Message}");
    }
  }
  
  public static async Task LogAsync(string message) {
    try {
      using var client = new TcpClient();
      await client.ConnectAsync(HOST, PORT);
            
      using var stream = client.GetStream();
      byte[] data = Encoding.UTF8.GetBytes(message + "\n");
      await stream.WriteAsync(data, 0, data.Length);
    }
    catch (Exception ex) {
      Console.WriteLine($"[TCP Log Failed]: {ex.Message}");
    }
  }
}