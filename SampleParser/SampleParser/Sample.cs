using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleParser
{
    /// <summary>
    /// Represents a PMU feature vector.
    /// </summary>
    [Serializable]
    public class Sample
    {
        public uint CycleCount = 0;
        public uint EventValue1 = 0;
        public uint EventValue2 = 0;
        public uint EventValue3 = 0;
        public uint EventValue4 = 0;
        public uint EventValue5 = 0;
        public uint EventValue6 = 0;

        /// <summary>
        /// Specifies if this sample came from a malware application
        /// or a non-malware application.
        /// </summary>
        public bool FromMalware;

        /// <summary>
        /// The sample file ID from which this vector originated.
        /// </summary>
        public long SampleFileId;

        /// <summary>
        /// The process name from which this vector originated.
        /// </summary>
        public string ProcessName;

        /// <summary>
        /// Returns whether or not this sample is a context switch (all values are zero).
        /// </summary>
        /// <returns>True if sample is context switch</returns>
        public bool IsContextSwitch()
        {
            return (CycleCount == 0 &&
                    EventValue1 == 0 &&
                    EventValue2 == 0 &&
                    EventValue3 == 0 &&
                    EventValue4 == 0 &&
                    EventValue5 == 0 &&
                    EventValue6 == 0);
        }
    }
}
