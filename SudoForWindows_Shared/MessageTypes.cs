//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using Google.Protobuf;
using System.IO;

namespace SudoForWindows_Shared
{
    public abstract class PipeEnabledObject
    {
        public abstract void LoadFromBytes(byte[] bytes);
        public abstract void WriteToStream(Stream stream);
    }

    public class SudoCommandRequest : PipeEnabledObject
    {
        private SudoCommandRequest_pb3 pipe_object = new SudoCommandRequest_pb3();

        public override void LoadFromBytes(byte[] bytes)
        {
            pipe_object = SudoCommandRequest_pb3.Parser.ParseFrom(bytes);
        }

        public string CommandString
        {
            get { return pipe_object.CommandString; }
            set { pipe_object.CommandString = value; }
        }
        public string RunAsUser
        {
            get { return pipe_object.RunAsUser; }
            set { pipe_object.RunAsUser = value; }
        }

        public override void WriteToStream(Stream stream)
        {
            pipe_object.WriteTo(stream);
        }
    }


    public enum OutputType
    {
        STDOUT = 0,
        STDERR = 1,
        SYSTEM = 2,
        EOF = 4
    }

    public class SudoCommandOutput : PipeEnabledObject
    {
        private SudoCommandOutput_pb3 pipe_object = new SudoCommandOutput_pb3();

        public override void LoadFromBytes(byte[] bytes)
        {
            pipe_object = SudoCommandOutput_pb3.Parser.ParseFrom(bytes);
        }

        public string OutputLine
        {
            get { return pipe_object.OutputLine; }
            set { pipe_object.OutputLine = value; }
        }
        public OutputType OutputType
        {
            get { return (OutputType)pipe_object.OutputType; }
            set { pipe_object.OutputType = (OutputType_pb3)value; }
        }

        public override void WriteToStream(Stream stream)
        {
            pipe_object.WriteTo(stream);
        }
    }
}