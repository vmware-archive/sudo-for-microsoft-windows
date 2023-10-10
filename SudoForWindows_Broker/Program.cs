//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace SudoForWindows_Broker
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            ServiceBase ServiceToRun = new SudoBroker(args);
            if (Environment.UserInteractive)
                RunInteractive(ServiceToRun);
            else
                ServiceBase.Run(ServiceToRun);
        }

        private static void RunInteractive(ServiceBase service)
        {
            Console.WriteLine("Service running in interactive mode.");
            Console.WriteLine();

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var onStartMethod = typeof(ServiceBase).GetMethod("OnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            Console.Write("Starting {0}...", service.ServiceName);
            onStartMethod.Invoke(service, new object[] { new string[] { } });
            Console.Write("Started");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(
                "Press any key to stop the service and end the process...");
            Console.ReadKey();
            Console.WriteLine();

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var onStopMethod = typeof(ServiceBase).GetMethod("OnStop",
                BindingFlags.Instance | BindingFlags.NonPublic);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            Console.Write("Stopping {0}...", service.ServiceName);
            onStopMethod.Invoke(service, null);
            Console.WriteLine("Stopped");

            Console.WriteLine("All services stopped.");
            // Keep the console alive for a second to allow the user to see the message.
            Thread.Sleep(1000);
        }
    }
}