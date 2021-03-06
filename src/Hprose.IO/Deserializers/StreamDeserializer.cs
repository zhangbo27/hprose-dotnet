﻿/*--------------------------------------------------------*\
|                                                          |
|                          hprose                          |
|                                                          |
| Official WebSite: https://hprose.com                     |
|                                                          |
|  StreamDeserializer.cs                                   |
|                                                          |
|  StreamDeserializer class for C#.                        |
|                                                          |
|  LastModified: Feb 8, 2019                               |
|  Author: Ma Bingyao <andot@hprose.com>                   |
|                                                          |
\*________________________________________________________*/

using System.IO;

namespace Hprose.IO.Deserializers {
    using static Tags;

    internal class StreamDeserializer<T> : Deserializer<T> where T : Stream {
        private static readonly byte[] empty = new byte[0];
        public override T Read(Reader reader, int tag) {
            var stream = reader.Stream;
            switch (tag) {
                case TagBytes:
                    return Converter<T>.Convert(ReferenceReader.ReadBytes(reader));
                case TagEmpty:
                    return Converter<T>.Convert(empty);
                case TagList:
                    return Converter<T>.Convert(ReferenceReader.ReadArray<byte>(reader));
                case TagUTF8Char:
                    return Converter<T>.Convert(ValueReader.ReadUTF8Char(stream));
                case TagString:
                    return Converter<T>.Convert(ReferenceReader.ReadChars(reader));
                default:
                    return base.Read(reader, tag);
            }
        }
    }
}
