using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
