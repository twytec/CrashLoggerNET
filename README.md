# CrashLogger for .NET
CrashLogger is a simple online logger for your Project.
[CrashLogher on NuGet](https://nuget.org)

## C#

```c#
using System;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CrashLogger.Logger.Init("263dbbed-e4b5-4f29-99f0-50367614f5b3");

            CrashLogger.Logger.Add("Name1", "Value1");

            var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test.txt");
            var stream = System.IO.File.OpenRead(file);

            CrashLogger.Logger.Add("Test.txt", stream);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);
                CrashLogger.Logger.Add("Test.txt", ms.ToArray());
            }

            CrashLogger.Logger.Send("Test");
        }
        
    }
}
```

## VB.NET
```vb
Imports System

Module Program
    Sub Main(args As String())
        CrashLogger.Logger.Init("263dbbed-e4b5-4f29-99f0-50367614f5b3")
        CrashLogger.Logger.Add("Name1", "Value1")
        Dim file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test.txt")
        Dim stream = System.IO.File.OpenRead(file)
        CrashLogger.Logger.Add("Test.txt", stream)

        Using ms As System.IO.MemoryStream = New System.IO.MemoryStream()
            stream.Position = 0
            stream.CopyTo(ms)
            CrashLogger.Logger.Add("Test.txt", ms.ToArray())
        End Using

        CrashLogger.Logger.Send("Test")
    End Sub
End Module
```

## License
MIT
