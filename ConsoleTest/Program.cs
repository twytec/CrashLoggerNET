using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CrashLogger.GlobalLogger.Init("01ad3554-6271-4c2d-ba6b-9d963b0e9ef5");

            CrashLogger.GlobalLogger.Log.Add("Name1", "Value1");

            var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test.txt");
            var stream = System.IO.File.OpenRead(file);

            CrashLogger.GlobalLogger.Log.Add("Test.txt", stream);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);
                CrashLogger.GlobalLogger.Log.Add("Test.txt", ms.ToArray());
            }

            CrashLogger.GlobalLogger.Log.Send("Test");

            //Or

            CrashLogger.Logger log = new CrashLogger.Logger("01ad3554-6271-4c2d-ba6b-9d963b0e9ef5");
            log.Add("Name1", "Value1");
            log.Send("Test");


            var la = 123;
        }
        
    }
}
