using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Samples.Debugging.MdbgEngine;
using Microsoft.Samples.Debugging.CorDebug;

namespace ConsoleApp1
{

    class Program
    {
        static void Main(string[] args)
        {
            var stop = new ManualResetEvent(false);
            var engine = new MDbgEngine();

            int pid = 0 ;
            int lineStart = 0;
            int lineEnd = 0;
            string fileName = "";

            try
            {
                pid = Int32.Parse(args[0]);

                lineStart = Int32.Parse(args[1]);
                lineEnd = Int32.Parse(args[2]);

                fileName = args[3];

            }
            catch
            {
                Console.WriteLine("Usage: qndprof <pid> <lineStart> <lineEnd> <filename>");
            }

            var process = engine.Attach(pid);

            for (var l = lineStart; l < lineEnd; l++)
            {
                process.Breakpoints.CreateBreakpoint(fileName, l);
            }

            process.Go().WaitOne();

            // Process will stop when debugger attaches
            process.StopEvent.WaitOne();

            Console.WriteLine("Attaching events");


            var stopwatch = new Stopwatch();
            process.PostDebugEvent +=
                (sender, e) =>
                    {
                        //Console.Write("Event: ");
                        //Console.WriteLine(e.CallbackType.ToString(), e.CallbackArgs.ToString());
                        if (e.CallbackType == ManagedCallbackType.OnBreakpoint)
                        {
                            // Do timer
                            stopwatch.Stop();
                            Console.Write(stopwatch.Elapsed);
                            Console.Write(" ");
                            Console.WriteLine(process.Threads.Active.CurrentSourcePosition.ToString());
                            CorBreakpointEventArgs a = (CorBreakpointEventArgs)e.CallbackArgs;
                            process.Go();
                            stopwatch.Reset();
                            stopwatch.Start();

                            // Delete the breakpoint to prevent loops from hitting it twice
                            process.Breakpoints.Lookup(a.Breakpoint).Delete();

                            if (process.Threads.Active.CurrentSourcePosition.Line == lineEnd)
                            {
                                stop.Set();
                            }
                        }

                        if (e.CallbackType == ManagedCallbackType.OnException2)
                        {
                            //Console.WriteLine("Exception: ", e.CallbackArgs.ToString(), e.CallbackArgs.GetType());
                            //ClrDump.CreateDump(process.CorProcess.Id, @"C:\temp.dmp", (int)MINIDUMP_TYPE.MiniDumpWithFullMemory, 0, IntPtr.Zero);
                        }

                        if (e.CallbackType == ManagedCallbackType.OnProcessExit)
                        {
                            Console.WriteLine("Exit");
                            stop.Set();
                        }
                    };

            process.Go();
            
            //while (true) {
            //    Console.WriteLine("Waiting for stop");
            //    process.StopEvent.WaitOne();
            //    Console.WriteLine("Stop Reason: ", process.StopReason.ToString());
            //    if (Console.ReadKey().KeyChar != 'n')
            //    {
            //        Console.WriteLine("exit");
            //        break;
            //    }

            //    process.Go();
            //}

            stop.WaitOne();

            process.AsyncStop().WaitOne();
            process.Detach();
        }
    }
}
