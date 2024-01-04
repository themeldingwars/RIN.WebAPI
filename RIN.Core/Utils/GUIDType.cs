using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core
{
    public enum GUIDType : byte
    {
        Character = 0xFE,
        V1        = 0x7F,
        Transient = 0xFF, // Map Entitys
        Items     = 0xFD
    }
}
