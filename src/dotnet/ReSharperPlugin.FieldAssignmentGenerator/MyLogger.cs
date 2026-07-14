using System.IO;

namespace ReSharperPlugin.FieldAssignmentGenerator;

public class MyLogger {
  private string m_content = "";
  
  public MyLogger Append(string message) {
    m_content += message;
    return this;
  }
  
  public MyLogger Log(string message) {
    m_content += message + "\n";
    return this;
  }

  public MyLogger Dump(string file) {
    File.WriteAllText(@"C:\temp\" + file, m_content);
    return this;
  }

  public MyLogger Clear() {
    m_content = "";
    return this;
  }
}