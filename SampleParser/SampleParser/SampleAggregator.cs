using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SampleParser
{
    /// <summary>
    /// Contains various methods to aggregate samples.
    /// </summary>
    public class SampleAggregator : ISampleAggregator
    {
        private const string UnknownProcessString = "Unknown";

        public event Action<string> DataAggregatedEvent;
        public event Action<List<string>> MultipleDataAggregatedEvent;

        public void AggregateData(List<Sample> samples, FeatureVectorType aggregateType, bool includeProcessInfo = false)
        {
            if (aggregateType == FeatureVectorType.SummedContextSwitch)
            {
                SumSamplesBetweenContextSwitches(samples, false, includeProcessInfo);
            }
            else if (aggregateType == FeatureVectorType.AverageContextSwitch)
            {
                AverageSamplesBetweenContextSwitches(samples, false, includeProcessInfo);
            }
            else if (aggregateType == FeatureVectorType.SummedContextSwitchWithTotalCountIncluded)
            {
                SumSamplesBetweenContextSwitches(samples, true, includeProcessInfo);
            }
            else if (aggregateType == FeatureVectorType.AverageContextSwithWithTotalCountIncluded)
            {
                AverageSamplesBetweenContextSwitches(samples, true, includeProcessInfo);
            }
            else if (aggregateType == FeatureVectorType.Histogram)
            {
                CreateHistogramFromSamples(samples, includeProcessInfo, false);
            }
            else if (aggregateType == FeatureVectorType.HistogramBetweenContextSwitches)
            {
                CreateHistogramFromSamples(samples, includeProcessInfo, true);
            }
            else if (aggregateType == FeatureVectorType.RawSamples)
            {
                List<string> sampleStrings = new List<string>();
                foreach (Sample sample in samples)
                {
                    string malwareString = (sample.FromMalware) ? Constants.MalwareString : Constants.NonMalwareString;
                    string sampleString =
                        sample.CycleCount + Constants.Delimiter +
                        sample.EventValue1 + Constants.Delimiter +
                        sample.EventValue2 + Constants.Delimiter +
                        sample.EventValue3 + Constants.Delimiter +
                        sample.EventValue4 + Constants.Delimiter +
                        sample.EventValue5 + Constants.Delimiter +
                        sample.EventValue6 + Constants.Delimiter +
                        malwareString;

                    sampleStrings.Add(sampleString);
                }

                if (MultipleDataAggregatedEvent != null)
                {
                    MultipleDataAggregatedEvent(sampleStrings);
                }
            }
        }

        private void CreateHistograms(List<Sample> samples, bool includeProcessInfo)
        {
            int buckets = Constants.BucketCountHistogram;
            List<uint> event1 = new List<uint>(Constants.SamplesToRead);
            List<uint> event2 = new List<uint>(Constants.SamplesToRead);
            List<uint> event3 = new List<uint>(Constants.SamplesToRead);
            List<uint> event4 = new List<uint>(Constants.SamplesToRead);
            List<uint> event5 = new List<uint>(Constants.SamplesToRead);
            List<uint> event6 = new List<uint>(Constants.SamplesToRead);
            foreach (var sample in samples)
            {
                event1.Add(sample.EventValue1);
                event2.Add(sample.EventValue2);
                event3.Add(sample.EventValue3);
                event4.Add(sample.EventValue4);
                event5.Add(sample.EventValue5);
                event6.Add(sample.EventValue6);
            }

            var binnedEvents1 = new Histogram(event1, buckets);
            var binnedEvents2 = new Histogram(event2, buckets);
            var binnedEvents3 = new Histogram(event3, buckets);
            var binnedEvents4 = new Histogram(event4, buckets);
            var binnedEvents5 = new Histogram(event5, buckets);
            var binnedEvents6 = new Histogram(event6, buckets);

            // Write the bins as a single feature vector
            List<Histogram> histoList = new List<Histogram>() { binnedEvents1, binnedEvents2, binnedEvents3, binnedEvents4, binnedEvents5, binnedEvents6 };
            StringBuilder builder = new StringBuilder();

            foreach (var histogram in histoList)
            {
                builder.Append(histogram.ToString());
            }

            string lineToAppend = builder.ToString();

            if (includeProcessInfo)
            {
                string procName = (!string.IsNullOrWhiteSpace(samples[0].ProcessName)) ? samples[0].ProcessName : UnknownProcessString;
                lineToAppend += procName + Constants.Delimiter +
                                samples[0].SampleFileId + Constants.Delimiter;
            }

            string malwareString = (samples[0].FromMalware) ? Constants.MalwareString : Constants.NonMalwareString;
            lineToAppend += malwareString;

            if (DataAggregatedEvent != null)
            {
                DataAggregatedEvent(lineToAppend);
            }
        }

        private void CreateHistogramFromSamples(List<Sample> samples, bool includeProcessInfo, bool createHistogramBetweenContextSwitches = false)
        {
            List<Sample> samplesToProcess = new List<Sample>(Constants.SamplesToRead);

            // Read samples into list, keep processing until context switch encountered or end of samples list
            int counter = 0;
            while (counter < samples.Count)
            {
                // if context switch encountered and createHistogramBetweenContextSwitches is true
                // start over on the samples to process list
                if (samples[counter].IsContextSwitch())
                {
                    if (createHistogramBetweenContextSwitches)
                    {
                        samplesToProcess.Clear();
                    }                    
                }
                else
                {
                    samplesToProcess.Add(samples[counter]);
                }              

                if (samplesToProcess.Count == Constants.SamplesToRead)
                {
                    // process samples
                    CreateHistograms(samplesToProcess, includeProcessInfo);
                    samplesToProcess.Clear();
                }

                counter++;
            }
        }       

        #region PMU Sample Aggregation Methods

        /// <summary>
        /// Given a samples list, sums the vectors between context switches and writes it to the dataset.
        /// </summary>
        /// <param name="samples">Samples list gathered from a single sample file</param>
        /// <param name="addSampleCountDimension">Adds another dimension to the vector that is written to the dataset.  
        /// This dimension is the number of samples that were logged between context switches.</param>
        private void SumSamplesBetweenContextSwitches(List<Sample> samples, bool addSampleCountDimension, bool includeProcessInfo = false)
        {
            ulong event1Sum = 0;
            ulong event2Sum = 0;
            ulong event3Sum = 0;
            ulong event4Sum = 0;
            ulong event5Sum = 0;
            ulong event6Sum = 0;

            ulong sampleCounter = 0;

            foreach (Sample sample in samples)
            {                
                if (sample.IsContextSwitch())
                {
                    // sum values until context switch detected
                    // when switch detected, log values to dataset file and reset counters
                    string malwareString = (sample.FromMalware) ? Constants.MalwareString : Constants.NonMalwareString;

                    string lineToAppend =
                            event1Sum + Constants.Delimiter +
                            event2Sum + Constants.Delimiter +
                            event3Sum + Constants.Delimiter +
                            event4Sum + Constants.Delimiter +
                            event5Sum + Constants.Delimiter +
                            event6Sum + Constants.Delimiter;

                    if (addSampleCountDimension)
                    {
                        lineToAppend += sampleCounter.ToString() + Constants.Delimiter;
                    }

                    if (includeProcessInfo)
                    {
                        string procName = (!string.IsNullOrWhiteSpace(sample.ProcessName)) ? sample.ProcessName : UnknownProcessString;
                        lineToAppend += procName + Constants.Delimiter +
                                        sample.SampleFileId + Constants.Delimiter;
                    }

                    lineToAppend += malwareString;

                    if (DataAggregatedEvent != null &&
                        !lineToAppend.Contains("0,0,0,0,0,0,"))
                    {
                        DataAggregatedEvent(lineToAppend);
                    }

                    event1Sum = 0;
                    event2Sum = 0;
                    event3Sum = 0;
                    event4Sum = 0;
                    event5Sum = 0;
                    event6Sum = 0;
                    sampleCounter = 0;
                }
                else
                {
                    event1Sum += sample.EventValue1;
                    event2Sum += sample.EventValue2;
                    event3Sum += sample.EventValue3;
                    event4Sum += sample.EventValue4;
                    event5Sum += sample.EventValue5;
                    event6Sum += sample.EventValue6;
                    sampleCounter++;
                }
            }
        }

        /// <summary>
        /// Given a samples list, averages the vectors between context switches and writes it to the dataset.
        /// </summary>
        /// <param name="samples">Samples list gathered from a single sample file</param>
        /// <param name="addSampleCountDimension">Adds another dimension to the vector that is written to the dataset.  
        /// This dimension is the number of samples that were logged between context switches.</param>
        private void AverageSamplesBetweenContextSwitches(List<Sample> samples, bool addSampleCountDimension, bool includeProcessInfo = false)
        {
            ulong event1Sum = 0;
            ulong event2Sum = 0;
            ulong event3Sum = 0;
            ulong event4Sum = 0;
            ulong event5Sum = 0;
            ulong event6Sum = 0;

            ulong sampleCounter = 0;

            foreach (Sample sample in samples)
            {
                if (sample.IsContextSwitch())
                {
                    if (sampleCounter == 0)
                    {
                        continue;
                    }

                    // sum values until context switch detected, then find average
                    // when switch detected, log values to dataset file and reset counters
                    ulong event1Average = event1Sum / sampleCounter;
                    ulong event2Average = event2Sum / sampleCounter;
                    ulong event3Average = event3Sum / sampleCounter;
                    ulong event4Average = event4Sum / sampleCounter;
                    ulong event5Average = event5Sum / sampleCounter;
                    ulong event6Average = event6Sum / sampleCounter;

                    string malwareString = (sample.FromMalware) ? Constants.MalwareString : Constants.NonMalwareString;

                    string lineToAppend =
                            event1Average + Constants.Delimiter +
                            event2Average + Constants.Delimiter +
                            event3Average + Constants.Delimiter +
                            event4Average + Constants.Delimiter +
                            event5Average + Constants.Delimiter +
                            event6Average + Constants.Delimiter;

                    if (addSampleCountDimension)
                    {
                        lineToAppend += sampleCounter.ToString() + Constants.Delimiter;
                    }

                    if (includeProcessInfo)
                    {
                        string procName = (!string.IsNullOrWhiteSpace(sample.ProcessName)) ? sample.ProcessName : UnknownProcessString;
                        lineToAppend += procName + Constants.Delimiter +
                                        sample.SampleFileId + Constants.Delimiter;
                    }

                    lineToAppend += malwareString;

                    if (DataAggregatedEvent != null)
                    {
                        DataAggregatedEvent(lineToAppend);
                    }

                    event1Sum = 0;
                    event2Sum = 0;
                    event3Sum = 0;
                    event4Sum = 0;
                    event5Sum = 0;
                    event6Sum = 0;
                    sampleCounter = 0;
                }
                else
                {
                    event1Sum += sample.EventValue1;
                    event2Sum += sample.EventValue2;
                    event3Sum += sample.EventValue3;
                    event4Sum += sample.EventValue4;
                    event5Sum += sample.EventValue5;
                    event6Sum += sample.EventValue6;
                    sampleCounter++;
                }
            }
        }

        #endregion
    }
}
