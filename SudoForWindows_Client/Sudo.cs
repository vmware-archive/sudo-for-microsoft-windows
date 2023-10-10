//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using SudoForWindows_Shared;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace SudoForWindows_Client
{
     static class Sudo
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Sudo for Windows");
            var command = new Argument<string>(
                "command",
                "The command to run via sudo"
            );
            var runas_user = new Option<string>(
                "--user",
                "The command to run via sudo"
            );
            runas_user.AddAlias("-u");
            runas_user.SetDefaultValue("");
            rootCommand.AddArgument(command);
            rootCommand.AddOption(runas_user);

            rootCommand.SetHandler(RunSudoCommand, command, runas_user);

            return await rootCommand.InvokeAsync(args);
        }

        private static Task<int> RunSudoCommand(string command, string user)
        {
            var return_code = 0;
            using (var pipeClient = new NamedPipeClientStream("sudo"))
            {
                try
                {
                    pipeClient.Connect(10000); // 10 Second timeout for connection to pipe
                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    SendCommandToPipe(command, user, pipeClient);

                    foreach (var output in ReadOutputFromPipe(pipeClient))
                        switch (output.OutputType)
                        {
                            case OutputType.STDOUT:
                                Console.WriteLine(output.OutputLine);
                                break;
                            case OutputType.STDERR:
                                Console.Error.WriteLine(output.OutputLine);
                                return_code = 1;
                                break;
                            case OutputType.SYSTEM:
                                Console.WriteLine("System Message: " + output.OutputLine);
                                return_code = 2;
                                break;
                        }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout occurred while trying to execute command. Ensure the Sudo broker service is running.");
                    return_code = 3;
                }
                catch (PipeTerminatedException)
                {
                    Console.WriteLine("WARNING: Sudo connection was terminated prematurely.");
                    return_code = 4;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WARNING: Unexpected error occurred.");
                    Console.WriteLine(ex.ToString());
                    return_code = 5;
                }
            }

            return Task.FromResult(return_code);
        }

        private static void SendCommandToPipe(string command, string runas_user, NamedPipeClientStream pipeClient)
        {
            var request = new SudoCommandRequest
            {
                CommandString = command,
                RunAsUser = runas_user
            };
            SendCommandToPipe(request, pipeClient);
        }

        private static void SendCommandToPipe(SudoCommandRequest request, NamedPipeClientStream pipeClient)
        {
            try
            {
                PipeFormatter.Serialize(pipeClient, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while trying to send command to broker.");
                Console.WriteLine(ex.ToString());
            }
            
        }

        private static IEnumerable<SudoCommandOutput> ReadOutputFromPipe(NamedPipeClientStream pipeClient)
        {
            SudoCommandOutput output;
            do
            {
                output = PipeFormatter.Deserialize<SudoCommandOutput>(pipeClient);
                yield return output;
                if (!pipeClient.IsConnected)
                {
                    throw new PipeTerminatedException();
                }
            } while (output.OutputType != OutputType.EOF);
        }
    }
}