using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleParser
{
    /// <summary>
    /// Represents a histogram.
    /// </summary>
    public class Histogram
    {
        private readonly List<uint> bins;
        public List<uint> Bins
        {
            get { return bins; }
        }

        public uint Min { get; private set; }
        public uint Max { get; private set; }
        public int TotalBins { get; private set; }
        public uint BinSize { get; private set; }

        public Histogram(List<uint> items, int binsArg)
        {
            TotalBins = binsArg;

            // Performs binning.  Reference: http://stackoverflow.com/questions/2387916/looking-for-a-histogram-binning-algorithm-for-decimal-data
            Min = items.Min();
            Max = items.Max();
            bins = new List<uint>(TotalBins);
            for (int i = 0; i < TotalBins; i++)
            {
                bins.Add(0);
            }

            BinSize = (Max - Min) / (uint)TotalBins;
            foreach (var value in items)
            {
                int bucketIndex = 0;
                if (BinSize > 0.0)
                {
                    bucketIndex = (int)((value - Min) / BinSize);
                    if (bucketIndex >= TotalBins)
                    {
                        bucketIndex = TotalBins - 1;
                    }
                }
                bins[bucketIndex]++;
            }
        }

        public override string ToString()
        {
            string rv = 
                BinSize.ToString() + Constants.Delimiter +
                Min.ToString() + Constants.Delimiter +
                Max.ToString() + Constants.Delimiter;

            foreach (uint count in bins)
            {
                rv += count.ToString() + Constants.Delimiter;
            }

            return rv;
        }
    }
}
