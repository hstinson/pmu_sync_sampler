using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleParser
{
    public class Constants
    {
        public const string MalwareString = "malware";
        public const string NonMalwareString = "non_malware";
        public const string Delimiter = ",";
        public const int BucketCountHistogram = 8;
        public const int SamplesToRead = 32;
    }
}
