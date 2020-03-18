using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace BrctcSpaceLibrary.Vibe2020Programs
{
    public interface ISingleDevice
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeLimit">Length of time to run the program</param>
        /// <returns>Tuple of the filename and legth of each segment</returns>
        public void Run(double timeLimit, System.Threading.CancellationToken token);

        public long GetDataSetCount();

        public string GetFileName();

        public int GetSegmentLength();

    }
}
