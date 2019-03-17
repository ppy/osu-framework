// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime.CompilerServices;
using osuTK;

namespace osu.Framework.Graphics.OpenGL
{
    public class DepthValue
    {
        private Half half;

        public DepthValue()
        {
            half = new Half(-1f);
        }

        public void Increment()
        {
            if (sign == 1)
            {
                switch (mantissa)
                {
                    case 0 when exponent == 0:
                        sign = 0;
                        mantissa++;
                        break;
                    case 0:
                        mantissa = 0x3FF;
                        exponent--;
                        break;
                    default:
                        mantissa--;
                        break;
                }
            }
            else
            {
                switch (mantissa)
                {
                    case 0 when exponent == 0xF: // Overflow, clamp to 1.0
                        break;
                    case 0x3FF:
                        mantissa = 0;
                        exponent++;
                        break;
                    default:
                        mantissa++;
                        break;
                }
            }
        }

        public static implicit operator float(DepthValue d) => d.half;

        private int sign
        {
            get => (getValue() & 0x8000) >> 15;
            set => setValue((ushort)((getValue() & 0x7FFF) | ((value << 15) & 0x8000)));
        }

        private int exponent
        {
            get => (getValue() & 0x7C00) >> 10;
            set => setValue((ushort)((getValue() & 0x83FF) | ((value << 10) & 0x7C00)));
        }

        private int mantissa
        {
            get => getValue() & 0x3FF;
            set => setValue((ushort)((getValue() & 0xFC00) | (value & 0x3FF)));
        }

        private unsafe void setValue(ushort value) => Unsafe.Write(Unsafe.AsPointer(ref half), value);

        private unsafe int getValue() => Unsafe.Read<ushort>(Unsafe.AsPointer(ref half));
    }
}
