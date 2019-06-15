using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

using OpenLibSys;

namespace RIM
{
    class Program
    {
        static string Uint_to_string(uint value)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(value));
        }

        static void WriteString(int line, int pos, string value)
        {
            Console.SetCursorPosition(pos, line);
            Console.Write(value);
        }

        static void WriteValue(int line, int pos, long value)
        {
            if (value > 999999999)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(pos, line);

            decimal d = (decimal)value / 1000000000;

            Console.Write(d.ToString("0.00" + " GI").PadLeft(8));
        }

        static void Main(string[] args)
        {
            try
            {
                Ols ols = new Ols();

                switch (ols.GetStatus())
                {
                    case (uint)Ols.Status.NO_ERROR:
                        break;
                    case (uint)Ols.Status.DLL_NOT_FOUND:
                        throw new Exception("Status Error!! DLL_NOT_FOUND");
                    case (uint)Ols.Status.DLL_INCORRECT_VERSION:
                        throw new Exception("Status Error!! DLL_INCORRECT_VERSION");
                    case (uint)Ols.Status.DLL_INITIALIZE_ERROR:
                        throw new Exception("Status Error!! DLL_INITIALIZE_ERROR");
                }

                // Check WinRing0 status
                switch (ols.GetDllStatus())
                {
                    case (uint)Ols.OlsDllStatus.OLS_DLL_NO_ERROR:
                        break;
                    case (uint)Ols.OlsDllStatus.OLS_DLL_DRIVER_NOT_LOADED:
                        throw new Exception("DLL Status Error!! OLS_DRIVER_NOT_LOADED");
                    case (uint)Ols.OlsDllStatus.OLS_DLL_UNSUPPORTED_PLATFORM:
                        throw new Exception("DLL Status Error!! OLS_UNSUPPORTED_PLATFORM");
                    case (uint)Ols.OlsDllStatus.OLS_DLL_DRIVER_NOT_FOUND:
                        throw new Exception("DLL Status Error!! OLS_DLL_DRIVER_NOT_FOUND");
                    case (uint)Ols.OlsDllStatus.OLS_DLL_DRIVER_UNLOADED:
                        throw new Exception("DLL Status Error!! OLS_DLL_DRIVER_UNLOADED");
                    case (uint)Ols.OlsDllStatus.OLS_DLL_DRIVER_NOT_LOADED_ON_NETWORK:
                        throw new Exception("DLL Status Error!! DRIVER_NOT_LOADED_ON_NETWORK");
                    case (uint)Ols.OlsDllStatus.OLS_DLL_UNKNOWN_ERROR:
                        throw new Exception("DLL Status Error!! OLS_DLL_UNKNOWN_ERROR");
                }
                
                if (Processor.Vendor != "AuthenticAMD")
                {
                    throw new Exception($"CPU Vendor {Processor.Vendor} not supported.");
                }

                if (Processor.Family != 23 )
                {
                    throw new Exception($"CPU Family {Processor.Family} not supported.");
                }

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                PerfMonThread.Init(Processor.LogicalCoreCount);

                Console.SetWindowSize(80, Processor.LogicalCoreCount);
                Console.CursorVisible = false;

                for (int i = 0; i < Processor.LogicalCoreCount; i++)
                {
                    if ( i % 4 < 2 )
                        Console.ForegroundColor = ConsoleColor.Gray;
                    else
                        Console.ForegroundColor = ConsoleColor.White;

                    WriteString(i, 1, i.ToString().PadLeft(3) + ":");
                    WriteString(i, 06, "IS:");
                    WriteString(i, 18, "BI:");
                    WriteString(i, 30, "LS:");
                    WriteString(i, 42, "FP:");
                    WriteString(i, 54, "FM:");
                    WriteString(i, 66, "OI:");
                }

                long[][] data;
                long rem;

                do
                {
                    data = PerfMonThread.PerfData;

                    for (int i = 0; i < Processor.LogicalCoreCount; i++)
                    {
                        WriteValue(i, 9, data[i][0]);
                        WriteValue(i, 21, data[i][1]);
                        WriteValue(i, 33, data[i][2]);
                        WriteValue(i, 45, data[i][3]);
                        WriteValue(i, 57, data[i][4]);

                        rem = data[i][0] - data[i][1] - data[i][2] - data[i][3] - data[i][4];

                        if (rem < 0)
                            rem = 0;

                        WriteValue(i, 69, rem);
                    }

                    Thread.Sleep(500);
                }
                while (!Console.KeyAvailable);

                PerfMonThread.StopWork = true;

                Thread.Sleep(2000);

                Console.CursorVisible = true;

                Environment.Exit(0);
            }
            catch ( Exception ex )
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Press any key to exit ...");
                Console.CursorVisible = true;
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
}
