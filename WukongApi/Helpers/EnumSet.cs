using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WukongApi.Helpers
{
    public sealed class EnumSet<T> : IEnumerable<T> where T : Enum
    {
        private readonly byte[] _flags = new byte[32];

        private EnumSet()
        {
            // check if Enum's max value is beyond 255, if so throw
            if (Enum.GetValues(typeof(T)).Length > 256)
            {
                throw new ArgumentException("EnumSet only supports enums with a maximum of 256 values");
            }
        }

        public EnumSet(IEnumerable<T> initial) : this()
        {
            foreach (var value in initial)
            {
                Add(value);
            }
        }

        public void Add(T value)
        {
            var index = Convert.ToInt32(value);
            _flags[index / 8] |= (byte)(1 << (index % 8));
        }

        public void Remove(T value)
        {
            var index = Convert.ToInt32(value);
            _flags[index / 8] &= (byte)~(1 << (index % 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value)
        {
            var index = Convert.ToInt32(value);
            return (_flags[index / 8] & (1 << (index % 8))) != 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _flags.Length; i++)
            {
                var flag = _flags[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((flag & (1 << j)) != 0)
                    {
                        yield return (T)Enum.ToObject(typeof(T), i * 8 + j);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}