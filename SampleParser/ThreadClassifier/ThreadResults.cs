using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SampleParser;

namespace ThreadClassifier
{
    public class ThreadResults : ThreadClassifier.IThreadResults
    {
        /// <summary>
        /// The number where if the thread results' malware classification count is greater
        /// than then that thread is classified as malware.
        /// </summary>
        public const int ClassifierCountThreshold = 1;   

        private static double classificationPercent = 0.05;

        public static void SetClassifcationThresholdPercent(int percentage)
        {
            classificationPercent = (double)percentage / 100.0;
        }

        public ThreadResults(int threadId, string procName, bool isMalware)
        {
            ThreadId = threadId;
            ProcessName = procName;
            IsMalware = isMalware;
        }

        public int ThreadId { get; private set; }
        public string ProcessName { get; private set; }
        public bool IsMalware { get; private set; }

        /// <summary>
        /// Amount of times thread classified as malware.
        /// </summary>
        public int ClassifiedAsMalwareCount { get; private set; }

        public int ContextSwitchesIdentifiedCorrectly { get; private set; }
        public int ContextSwitchesIdentifiedIncorrectly { get; private set; }
        public int TotalPredictions
        {
            get
            {
                return ContextSwitchesIdentifiedCorrectly + ContextSwitchesIdentifiedIncorrectly;
            }
        }

        public double CorrectPercentage
        {
            get
            {
                double rv = (double)ContextSwitchesIdentifiedCorrectly / (double)TotalPredictions;
                rv *= 100;
                return rv;
            }
        }

        public double IncorrectPercentage
        {
            get
            {
                double rv = (double)ContextSwitchesIdentifiedIncorrectly / (double)TotalPredictions;
                rv *= 100;
                return rv;
            }
        }

        public void AddPredictionInfo(string correctLabel, string predictedLabel, double predictedMalware = -1.0d, double predictedNonMalware = -1.0d)
        {
            if (Equals(correctLabel, predictedLabel))
            {
                ContextSwitchesIdentifiedCorrectly++;
            }
            else
            {
                ContextSwitchesIdentifiedIncorrectly++;
            }

            if (Equals(predictedLabel, Constants.MalwareString))
            {
                ClassifiedAsMalwareCount++;
            }
        }

        public bool IsClassifiedAsMalware()
        {
            bool isMalware = false;

            // Simple classification method, needs updating.
            if (ClassifiedAsMalwareCount > TotalPredictions / 2)
            {
                isMalware = true;
            }

            return isMalware;
        }
    }
}
