using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleParser
{
    public static class ListExtensionMethods
    {
        //Ref: http://stackoverflow.com/questions/2387916/looking-for-a-histogram-binning-algorithm-for-decimal-data
        public static List<uint> Bucketize(this IEnumerable<uint> source, int totalBuckets)
        {
            var min = source.Min();
            var max = source.Max();
            var buckets = new List<uint>(totalBuckets);
            for (int i = 0; i < totalBuckets; i++)
            {
                buckets.Add(0);
            }

            var bucketSize = (max - min) / totalBuckets;
            foreach (var value in source)
            {
                int bucketIndex = 0;
                if (bucketSize > 0.0)
                {
                    bucketIndex = (int)((value - min) / bucketSize);
                    if (bucketIndex >= totalBuckets)
                    {
                        bucketIndex = totalBuckets - 1;
                    }
                }
                buckets[bucketIndex]++;
            }
            return buckets;
        }
    }
}
