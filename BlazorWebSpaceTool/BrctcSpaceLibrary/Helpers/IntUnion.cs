using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BrctcSpaceLibrary.Helpers
{
    // Declare a struct called IntUnion that will store an integer and its individual bytes
    [StructLayout(LayoutKind.Explicit)]
    public struct IntUnion
    {
        // Declare a byte at offset 0
        [FieldOffset(0)]
        public byte byte0;

        // Declare a byte at offset 1
        [FieldOffset(1)]
        public byte byte1;

        // Declare a byte at offset 2
        [FieldOffset(2)]
        public byte byte2;

        // Declare a byte at offset 3
        [FieldOffset(3)]
        public byte byte3;

        // Declare an integer that starts at offset 0 and overlaps with the byte fields
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
     *          
     *          ||||||||    ||||||||    ||||||||    ||||||||
     */
}


/*

This code defines a custom struct called `IntUnion` in the namespace `BrctcSpaceLibrary.Helpers`. 
The purpose of this struct is to allow you to access and manipulate the individual bytes of a 32-bit integer while also being able to treat it as a whole integer.

The `IntUnion` struct is marked with the `[StructLayout(LayoutKind.Explicit)]` attribute, which means that you can explicitly control the memory layout of the struct's fields. 
This is useful in situations where you need fine-grained control over the memory representation of data, like when interfacing with native code or when you want to access the individual bytes of a larger data type.

The struct contains four byte fields (byte0, byte1, byte2, byte3) and one integer field (integer). 
The `[FieldOffset()]` attribute is used to specify the memory offset for each field. The byte fields are placed at offsets 0, 1, 2, and 3, while the integer field starts at offset 0 and overlaps with the byte fields.

This overlapping allows you to access and manipulate the integer as either a whole integer or as individual bytes. 
For example, you can change the value of one of the bytes and see its effect on the integer value, or you can set the integer value and then inspect its bytes.

Using this custom struct has a performance advantage over some other native .NET methods, such as BitConverter, which involves creating temporary arrays and copying data. 
By directly mapping the memory representation of the integer and its bytes, the `IntUnion` struct allows for faster manipulation and conversion of the data,
which is particularly beneficial when working with large data sets or performance-critical applications.

In summary, the `IntUnion` struct is a convenient and efficient way to work with an integer and its bytes simultaneously.

*/