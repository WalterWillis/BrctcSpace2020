using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BrctcSpaceLibrary.Helpers
{
    [StructLayout(LayoutKind.Explicit)]
    public struct IntUnion
    {
        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;

        [FieldOffset(0)]
        public int integer;
    }

    //Visual representation
    /*          Field1      Field2      Field3      Field4
     *          byte0
     *                      byte1
     *                                  byte2
     *                                              byte3
     *                                              
     *          integer     integer     integer     integer
     */
}
