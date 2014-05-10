using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SampleParser;

namespace ThreadClassifier
{
    // Contains information about a thread as it relates to classification.
    public class ThreadResultsEnhanced : ThreadClassifier.IThreadResults
    {
        public ThreadResultsEnhanced(int threadId, string procName, bool isMalware)
        {
            ThreadId = threadId;
            ProcessName = procName;
            IsMalware = isMalware;
        }

        public int ThreadId { get; private set; }
        public string ProcessName { get; private set; }
        public bool IsMalware { get; private set; }

        private int totalPredictions;
        public int TotalPredictions
        {
            get
            {
                return totalPredictions;
            }
        }

        public double PredictedMalwareAverage
        {
            get
            {
                double average = predictedMalwareSum / (double)TotalPredictions;
                return average;
            }
        }

        public double PredictedNonMalwareAverage
        {
            get
            {
                double average = predictedNonMalwareSum / (double)TotalPredictions;
                return average;
            }
        }

        private double highestMalwarePrediction = 0.0;
        public double HighestMalwarePrediction
        {
            get
            {
                return highestMalwarePrediction;
            }
        }

        private double highestNonMalwarePrediction = 0.0;
        public double HighestNonMalwarePrediction
        {
            get
            {
                return highestNonMalwarePrediction;
            }
        }

        private double predictedMalwareSum;
        private double predictedNonMalwareSum;
        private double malwareScore = 0;
        private double nonMalwareScore = 0;

        private int falsePositives = 0;
        private int truePositives = 0;

        public int PredictedMalwareCount
        {
            get;
            private set;
        }

        public int PredictedNonMalwareCount
        {
            get;
            private set;
        }
        
        private void UpdateMetrics(string correctLabel, string predictedLabel, double predictedMalware, double predictedNonMalware)
        {
            if (Equals(Constants.MalwareString, correctLabel))  // Malware
            {
                if (Equals(Constants.MalwareString, predictedLabel))
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }
            }
            else // Non malware
            {
                if (Equals(Constants.NonMalwareString, predictedLabel))
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }
            }
        }

        public void AddPredictionInfo(string correctLabel, string predictedLabel, double predictedMalware = -1.0d, double predictedNonMalware = -1.0d)
        {
            UpdateMetrics(correctLabel, predictedLabel, predictedMalware, predictedNonMalware);

            totalPredictions++;

            // Aggregate (sum, then average the predicted values)
            // Determine percentage now or wait until later?
            predictedMalwareSum += predictedMalware;
            predictedNonMalwareSum += predictedNonMalware;

            if (predictedMalware > highestMalwarePrediction)
            {
                highestMalwarePrediction = predictedMalware;
            }

            if (predictedNonMalware > highestNonMalwarePrediction)
            {
                highestNonMalwarePrediction = predictedNonMalware;
            }

            if (Equals(Constants.MalwareString, predictedLabel))
            {
                PredictedMalwareCount++;
            }
            else if (Equals(Constants.NonMalwareString, predictedLabel))
            {
                PredictedNonMalwareCount++;
            }
            else
            {
                Console.WriteLine("ERROR: Invalid class name encountered!");
            }

            if (Equals(Constants.MalwareString, predictedLabel))
            {
                malwareScore += predictedMalware * 100;

                if (predictedMalware > HighPredictionRateThreshold)
                {
                    //malwareScore += predictedMalware * 600;
                    highMalwarePredictionCount++;
                }
            }
            else
            {
                nonMalwareScore += predictedNonMalware * 100;

                if (predictedNonMalware > HighPredictionRateThreshold)
                {
                    highNonMalwarePredictionCount++;
                }
            }
        }

        private const double HighPredictionRateThreshold = 0.85;
        private const int HighPredictionTriggerCount = 2;

        private int highMalwarePredictionCount = 0;
        private int highNonMalwarePredictionCount = 0;

        public bool IsClassifiedAsMalware()
        {
            bool isMalware = (malwareScore*2 >= nonMalwareScore);

            // Simple classfification technique.  Needs to be updated
            if ((double)PredictedMalwareCount / (double)TotalPredictions >= 0.33)
            {
                isMalware = true;
            }
            else
            {
                isMalware = false;
            }
           
            return isMalware;
        }
    }
}
