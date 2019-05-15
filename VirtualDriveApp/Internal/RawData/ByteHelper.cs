using System;

namespace VirtualDrive.Internal.RawData
{
    internal static class ByteHelper
    {
        public static int GetLength<T>() where T : struct
        {
            var value = default(T);
            return GetLength(value);
        }

        public static int GetLength<T>(T value) where T : struct
        {
            switch ((object)value)
            {
                case byte _:
                    return 1;
                case int _:
                case uint _:
                case float _:
                    return 4;

                case long _:
                case ulong _:
                case double _:
                case DateTime _:
                    return 8;

                case decimal _:
                    return 16;

                default:
                    throw new InvalidOperationException($"Type {typeof(T)} is not supported");
            }
        }
    }
}