//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using System;
using System.Diagnostics;
using System.Security;

namespace SudoForWindows_Broker
{
    internal static class Logger
    {
        private static bool isService;
        private static EventLog LogSender;

        internal static void InitializeLogging()
        {
            using (var myProcess = Process.GetCurrentProcess())
            {
                isService = myProcess.SessionId == 0;
            }

            if (isService)
            {
                if (!EventLog.SourceExists(Resources.EventLogName))
                    try
                    {
                        EventLog.CreateEventSource(Resources.EventLogName, "Application");
                    }
                    catch (SecurityException ex)
                    {
                        throw new SystemException(
                            $"Event log source \"{Resources.EventLogName}\" does not exist and could not be created. Run service as System user, or manually create source.",
                            ex);
                    }

                LogSender = new EventLog
                {
                    Source = Resources.EventLogName
                };
            }
            else
            {
                Console.WriteLine("SudoBroker is not running as a service. Logging will use Console");
            }
        }

        internal static void LogEvent(string message, EventLogEntryType type)
        {
            if (isService)
                LogSender.WriteEntry(message, type);
            else
                Console.WriteLine("[{0}] {1}", type.ToString(), message);
        }
    }
}