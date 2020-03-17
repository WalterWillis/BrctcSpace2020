using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Vibe2020Client.RegisterHelpers
{
    public static class SettingsValidator
    {

        //verify unused registers are unset
        public static bool AreSettingsValid(short data)
        {
            bool isValid = IsBitUnset(data, 4) &&
            IsBitUnset(data, 5) &&
            IsBitUnset(data, 8) &&
            IsBitUnset(data, 9) &&
            IsBitUnset(data, 10) &&
            IsBitUnset(data, 11) &&
            IsBitUnset(data, 12) &&
            IsBitUnset(data, 13) &&
            IsBitUnset(data, 14) &&
            IsBitUnset(data, 15);


            return isValid;
        }

        /// <summary>
        /// Check and see if the bit at the given index is unset
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool IsBitUnset(short value, byte index)
        {
            return !IsBitSet(value, index);
        }

        /// <summary>
        /// Checks to see if the bit is set at the guveb index
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool IsBitSet(short value, byte index)
        {
            //If nonzero, it is set
            return (value & (0 << index)) != 0;
        }
    }
}
