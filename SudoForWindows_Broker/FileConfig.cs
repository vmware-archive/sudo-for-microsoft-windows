//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using SudoForWindows_Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SudoForWindows_Broker
{
    internal enum PrincipalType
    {
        User,
        Group
    }

    internal class ConfigEntry
    {
        public string command;
        public string hostname;
        public string principal;
        public PrincipalType principalType;
        public bool require_password;
        public string[] runas_users;

        public bool CheckPrincipal(string requestingUser)
        {
            if (principal == "ALL") return true;

            //TODO: Investigate using PrincipalSearcher for this logic
            if (principalType == PrincipalType.Group)
                try
                {
                    using (var d = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer"))
                    {
                        using (var g = d.Children.Find(principal, "group"))
                        {
                            var members = g.Invoke("Members", null);
                            foreach (var member in (IEnumerable)members)
                            {
                                var x = new DirectoryEntry(member);
                                if (x.Name == requestingUser) return true;
                            }
                        }
                    }
                }
                catch (COMException)
                {
                    Logger.LogEvent($"Non-existing local group [{principal}] referenced in sudoers config.",
                        EventLogEntryType.Warning);
                }
            else if (principal.StartsWith("&"))
                try
                {
                    using (var d = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer"))
                    {
                        using (var g = d.Children.Find(principal, "user"))
                        {
                            if (g.Name == requestingUser) return true;
                        }
                    }
                }
                catch (COMException)
                {
                    Logger.LogEvent($"Non-existing local user [{principal}] referenced in sudoers config.",
                        EventLogEntryType.Warning);
                }
            else
                return requestingUser == principal;

            return false;
        }

        public bool CheckHostname()
        {
            if (hostname == "ALL") return true;

            return Environment.GetEnvironmentVariable("COMPUTERNAME").ToLower() == hostname.ToLower();
        }

        public bool CheckRunas(string RunAsUser)
        {
            if (runas_users.Contains("ALL")) return true;

            return runas_users.Contains(RunAsUser);
        }

        public bool CheckCommand(string request_command)
        {
            var allowed_executable = Path.GetFileName(command);
            if (allowed_executable.Contains(' ')) // Command contains arguments. Arguments must be exact match
                return command.ToLower() == request_command.ToLower();

            // Command does not contain arguments. Only path + executable must match
            var requested_command_directory = Path.GetDirectoryName(request_command);
            var allowed_command_directory = Path.GetDirectoryName(command);
            if (requested_command_directory.ToLower() == allowed_command_directory.ToLower())
            {
                var requested_command_executable = Path.GetFileName(request_command).Split(' ')[0];
                return requested_command_executable.ToLower() == allowed_executable.ToLower();
            }

            return false;
        }
    }

    internal abstract class Config
    {
        protected Config()
        {
        }

        protected Config(string[] ConfigFileContents, string ConfigFilePath)
        {
            Filename = ConfigFilePath;
#pragma warning disable S1699 // Constructors should only call non-overridable methods
            ConfigEntries = Parse(ConfigFileContents);
#pragma warning restore S1699 // Constructors should only call non-overridable methods
        }

        public abstract string FileHeader { get; }
        public ConfigEntry[] ConfigEntries { get; }
        public string Filename { get; private set; }

        public static Config IdentifyAndParse(string ConfigFile)
        {
            var config_lines = File.ReadLines(ConfigFile).ToArray();
            var derived_types = new List<Type>();
            foreach (var domain_assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assembly_types = domain_assembly.GetTypes()
                    .Where(type => type.IsSubclassOf(typeof(Config)) && !type.IsAbstract);

                derived_types.AddRange(assembly_types);
            }

            string header;
            foreach (var subclass in derived_types)
            {
                header = ((Config)Activator.CreateInstance(subclass)).FileHeader;
                if (config_lines[0].StartsWith(header))
                    return (Config)Activator.CreateInstance(subclass, config_lines, ConfigFile);
            }

            throw new NotSupportedException(
                $"File with header line [{config_lines[0]}] is not supported at this time.");
        }

        public abstract ConfigEntry[] Parse(string[] ConfigFileContents);

        public bool CheckCommandRequest(SudoCommandRequest request, string requestingUser)
        {
            foreach (var entry in ConfigEntries)
                if (
                    entry.CheckPrincipal(requestingUser) &&
                    entry.CheckHostname() &&
                    entry.CheckRunas(request.RunAsUser) &&
                    !entry.require_password &&
                    entry.CheckCommand(request.CommandString)
                )
                    return true;
            return false;
        }
    }


    /* Sudoers Config File Format: [%group|user] [hostname|ALL]=([ALL|runas_user[,runas_user,…]]) [NOPASSWD:] <absolute-program-path> [args]
        * Examples
        * ===========
        * %Users ALL=(ALL) NOPASSWD: C:\Windows\System32\whoami.exe
        * %Users ALL=(ALL) NOPASSWD: C:\Windows\System32\PING.EXE localhost
        */
    internal class SudoersV1Config : Config
    {
        private readonly Regex config_matcher =
            new Regex(
                "^(?<is_group>%)?(?<principal>\\w+) (?<hostname>\\w+)=\\((?<runas_users>[\\w,]+)+\\)(?<require_password> NOPASSWD:)? (?<command>.*)$");

        public SudoersV1Config()
        {
        }

        public SudoersV1Config(string[] ConfigFileContents, string ConfigFilePath) : base(ConfigFileContents,
            ConfigFilePath)
        {
        }

        public override string FileHeader => "# sudo.exe file format: 1.0";

        public override ConfigEntry[] Parse(string[] ConfigFileContents)
        {
            var entries = new List<ConfigEntry>();
            foreach (var line in ConfigFileContents)
            {
                if (line.StartsWith("#")) continue;

                if (config_matcher.IsMatch(line))
                {
                    var match = config_matcher.Match(line);
                    entries.Add(new ConfigEntry
                    {
                        principalType = match.Groups["is_group"].Success ? PrincipalType.Group : PrincipalType.User,
                        principal = match.Groups["principal"].Value,
                        hostname = match.Groups["hostname"].Value,
                        runas_users = match.Groups["runas_users"].Value.Split(',').ToArray(),
                        require_password = !match.Groups["require_password"].Success,
                        command = match.Groups["command"].Value
                    });
                }
            }

            return entries.ToArray();
        }
    }
}