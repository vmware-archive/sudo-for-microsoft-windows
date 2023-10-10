//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using SudoForWindows_Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

namespace SudoForWindows_Broker
{
    public partial class SudoBroker : ServiceBase
    {
        private Config configuration;
#pragma warning disable S4487 // Unread "private" fields should be removed
#pragma warning disable IDE0052 // Remove unread private members
        private IAsyncResult pipeWaiter;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore S4487 // Unread "private" fields should be removed
        private NamedPipeServerStream sudoPipe;
        private readonly string config_location;

        public SudoBroker(string[] args)
        {
            config_location = "./sudoers.conf";
            if (args.Length >= 1)
            {
                config_location = args[0];
            }
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {

            Logger.InitializeLogging();
            Logger.LogEvent("Starting Sudo Broker service", EventLogEntryType.Information);
            LoadConfiguration(config_location);


            var sudoPipeSecurity = new PipeSecurity();
            sudoPipeSecurity.AddAccessRule(
                new PipeAccessRule("Users",
                    PipeAccessRights.ReadWrite,
                    AccessControlType.Allow)
            );
            sudoPipe = new NamedPipeServerStream(
                "sudo",
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                0, 0,
                sudoPipeSecurity,
                HandleInheritability.None,
                PipeAccessRights.ChangePermissions
            );
            Logger.LogEvent("Created Sudo named pipe. Awaiting connections.", EventLogEntryType.Information);
            pipeWaiter = sudoPipe.BeginWaitForConnection(ConnectionEstablishedCallback, null);
        }

        private void LoadConfiguration(string config_location)
        {
            try
            {
                configuration = Config.IdentifyAndParse(config_location);
            }
            catch (FileNotFoundException)
            {
                Logger.LogEvent($"Configuration [{config_location}] not found. Service cannot start.",
                    EventLogEntryType.Error);
                ExitCode = -1;
                throw;
            }
        }

        protected void ConnectionEstablishedCallback(IAsyncResult asyncResult)
        {
            try
            {
                sudoPipe.EndWaitForConnection(asyncResult);
                var command = PipeFormatter.Deserialize<SudoCommandRequest>(sudoPipe);
                if (command.RunAsUser == null) command.RunAsUser = WindowsIdentity.GetCurrent().Name;

                var user = sudoPipe.GetImpersonationUserName();
                Logger.LogEvent($"Connection established from user {user}", EventLogEntryType.Information);
                if (configuration.CheckCommandRequest(command, user))
                {
                    Logger.LogEvent(
                        $"Executing command [{command.CommandString}] as [{command.RunAsUser}] for [{user}]",
                        EventLogEntryType.SuccessAudit);
                    ExecuteCommand(command);
                }
                else
                {
                    Logger.LogEvent(
                        $"User [{user}] requested non-matching command [{command.CommandString}] as [{command.RunAsUser}]",
                        EventLogEntryType.Warning);

                    var rejected = new SudoCommandOutput
                    {
                        OutputType = OutputType.SYSTEM,
                        OutputLine =
                        $"Sudo request failed. requested non-matching command [{command.CommandString}] as [{command.RunAsUser}]"
                    };
                    PipeFormatter.Serialize(sudoPipe, rejected);
                    var eof = new SudoCommandOutput
                    {
                        OutputType = OutputType.EOF,
                        OutputLine = "EOF"
                    };
                    PipeFormatter.Serialize(sudoPipe, eof);
                }

                sudoPipe.WaitForPipeDrain();
                sudoPipe.Disconnect();
            }
            catch (ObjectDisposedException)
            {
                // If the pipe is closed before a client ever connects,
                // EndWaitForConnection() will throw an exception.
                // If we are in here that is probably the case so just return.
            }
            finally
            {
                pipeWaiter = sudoPipe.BeginWaitForConnection(ConnectionEstablishedCallback, null);
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            try
            {
                var output = new SudoCommandOutput
                {
                    OutputType = OutputType.STDOUT,
                    OutputLine = outLine.Data ?? ""
                };
                PipeFormatter.Serialize(sudoPipe, output);
            }
            catch (Exception ex) 
            {
                Logger.LogEvent("Exception occurred while sending pipe output.", EventLogEntryType.Error);
                Logger.LogEvent(ex.ToString(), EventLogEntryType.Error);
                var output = new SudoCommandOutput
                {
                    OutputType = OutputType.SYSTEM,
                    OutputLine = "An error occurred while handling command output."
                };
                PipeFormatter.Serialize(sudoPipe, output);
            }
        }

        protected void ExecuteCommand(SudoCommandRequest request)
        {
            // Create Process definition
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {request.CommandString}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            // Set output and error handlers
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;
            // Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            var output = new SudoCommandOutput
            {
                OutputType = OutputType.EOF,
                OutputLine = "EOF"
            };
            PipeFormatter.Serialize(sudoPipe, output);
        }

        protected override void OnStop()
        {
            Logger.LogEvent("Stopping Sudo Broker service", EventLogEntryType.Information);
            eventLogSender.Dispose();
            sudoPipe.Close();
            Logger.LogEvent("Stopped Sudo Broker service", EventLogEntryType.Information);
        }
    }
}