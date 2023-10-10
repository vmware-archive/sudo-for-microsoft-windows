//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
using System;
using System.IO;
using System.IO.Pipes;

namespace SudoForWindows_Shared
{
    public class PipeFormatter
    {
        private static readonly Lazy<PipeFormatter> lazy =
            new Lazy<PipeFormatter>(() => new PipeFormatter());

        private PipeFormatter()
        {
        }

        public static PipeFormatter Instance => lazy.Value;

        public static void Serialize(PipeStream stream, PipeEnabledObject obj)
        {
                obj.WriteToStream(stream);
        }

        public static T Deserialize<T>(PipeStream stream) where T : PipeEnabledObject, new()
        {
            var messageBuffer = new byte[1024];
            int bytes;
            using (var memory = new MemoryStream())
            {
                do
                {
                    bytes = stream.Read(messageBuffer, 0, messageBuffer.Length);
                    memory.Write(messageBuffer, 0, bytes);
                } while (!stream.IsMessageComplete);

                memory.Seek(0, SeekOrigin.Begin);
                var obj = new T();
                obj.LoadFromBytes(memory.ToArray());
                return obj;
            }
        }
    }
}