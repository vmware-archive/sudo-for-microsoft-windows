# Sudo For Microsoft Windows

## Note: Active development on this project has ceased as of Feb 2024, and the project will no longer be maintained.

## Current Project status - End of Life

## Description
Sudo for Microsoft Windows aims to provide a familiar interface for allowing escalation of specific commands from within a Windows automation workflow. 
Our primary target is automated workflows, such as software builds and CI/CD pipelines, where elevation might be needed for a few operations, while most can be run
within the privileges of a limited user. The native Windows methods of achieving this elevation (User Account Control, RunAs, etc) tend to have significant
drawbacks when applied to automated processes. For example, allowing elevation via User Account Control is limited to a simple binary allow/deny for the 
elevation privilege, without very limited means of controlling what is run with the elevated privileges. 

The eventual goal of the Sudo For Microsoft Windows project is to provide a means by which a software build can have a verifiable, secure method of specifying
which commands will need escalation during the build, meaning that even if an attacker is able to compromise the build process itself, they are not able to
escalate privileges on the system and take full control without also compromising the configuration and potentially even signing keys used to secure the configuration.

## Usage
Current usage is limited to the following format:

`> sudo.exe "C:\Path\To\Binary.exe argument1 argument2 -argument3..."`

Please note that the full path to the binary (and any arguments specified) must match what is configured in the sudoers configuration file.

## Client Return Codes
|Code|Meaning|
|-----|---------|
|0|Command completed successfully|
|1|Command exited with error|
|2|Error in Sudo Broker|
|3|Timeout while connecting to broker|
|4|Broker connection terminated prematurely|
|5|Unknown error|

## Contributing

The sudo-for-microsoft-windows project team welcomes contributions from the community. Before you start working with sudo-for-microsoft-windows, please
read our [Developer Certificate of Origin](https://cla.vmware.com/dco). All contributions to this repository must be
signed as described on that page. Your signature certifies that you wrote the patch or have the right to pass it on
as an open-source patch. For more detailed information, refer to [CONTRIBUTING.md](CONTRIBUTING.md).


