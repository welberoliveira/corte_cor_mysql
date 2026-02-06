using System;
using System.IO;

public class Log
{
    public void Write(string message)
    {
        using (StreamWriter writer = new StreamWriter("log.txt", true))
        {
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}