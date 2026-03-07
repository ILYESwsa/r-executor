using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

Console.Title = "Safe Script Terminal";
Console.WriteLine("Safe Script Terminal started.");
Console.WriteLine("Listening for local INJECT messages from SafeScriptStudio...");
Console.WriteLine();

while (true)
{
    using var server = new NamedPipeServerStream("SafeScriptStudioPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None);
    server.WaitForConnection();

    using var reader = new StreamReader(server, Encoding.UTF8);
    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[TERMINAL] ");
        Console.ResetColor();
        Console.WriteLine(line);
    }
}
