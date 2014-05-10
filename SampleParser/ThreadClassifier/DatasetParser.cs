using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SampleParser;

namespace ThreadClassifier
{    
    public class DatasetParser
    {
        /// <summary>
        /// Maps thread ID to a thread results object.
        /// </summary>
        private readonly Dictionary<int, IThreadResults> threadResultsMap = new Dictionary<int, IThreadResults>();

        public void ParseDataset(string testDatasetFile, string outputFileName, string familyMapFileName)
        {
            bool usingThreadResultsEnhanced = false;
            using (var datasetReader = new StreamReader(testDatasetFile))
            {
                // skip past inital items in ARFF file
                int count = 0;
                string dummy;
                while ((dummy = datasetReader.ReadLine()) != "@data")
                {
                    if (count > 100)
                    {
                        // data string not found, exit out of loop, file is bad
                        break;
                    }
                    count++;
                }
               
                while (!datasetReader.EndOfStream)
                {                   
                    double predictedMalware, predictedNonMalware;
                    var splits = datasetReader.ReadLine().Split(',');
                    if (splits.Length == 10)  // 6 PMU events, process name, thread ID, correct label, predicted label
                    {
                        string processName = splits[6];
                        string threadIdString = splits[7];
                        string correctLabel = splits[8];
                        string predictedLabel = splits[9];

                        int threadId;
                        if (int.TryParse(threadIdString, out threadId))
                        {
                            if (threadResultsMap.ContainsKey(threadId))
                            {
                                // Add context switch info to existing thread object
                                threadResultsMap[threadId].AddPredictionInfo(correctLabel, predictedLabel);
                            }
                            else
                            {
                                // Add new thread info object, then add context switch info
                                bool isMalware = Equals(correctLabel, Constants.MalwareString);
                                ThreadResults results = new ThreadResults(threadId, processName, isMalware);
                                results.AddPredictionInfo(correctLabel, predictedLabel);
                                threadResultsMap.Add(threadId, results);
                            }
                        }                        
                    }
                    else if (splits.Length >= 12 &&
                             double.TryParse(splits[splits.Length - 2], out predictedMalware) &&
                             double.TryParse(splits[splits.Length - 1], out predictedNonMalware))  // We have a histogram or have prediction labels
                    {
                        usingThreadResultsEnhanced = true;

                        string processName = splits[splits.Length - 6];
                        string threadIdString = splits[splits.Length - 5];
                        string correctLabel = splits[splits.Length - 4];
                        string predictedLabel = splits[splits.Length - 3];

                        int threadId;
                        if (int.TryParse(threadIdString, out threadId))
                        {
                            if (threadResultsMap.ContainsKey(threadId))
                            {
                                // Add context switch info to existing thread object
                                threadResultsMap[threadId].AddPredictionInfo(correctLabel, predictedLabel, predictedMalware, predictedNonMalware);
                            }
                            else
                            {
                                // Add new thread info object, then add context switch info
                                bool isMalware = Equals(correctLabel, Constants.MalwareString);
                                ThreadResultsEnhanced results = new ThreadResultsEnhanced(threadId, processName, isMalware);
                                results.AddPredictionInfo(correctLabel, predictedLabel, predictedMalware, predictedNonMalware);
                                threadResultsMap.Add(threadId, results);
                            }
                        }    
                    }
                    else
                    {
                        Console.WriteLine("Warning: Encountered line in dataset file that does not contain the correct amount of attributes and labels.");
                    }
                }
            }

            // Done reading dataset file, now perform writing information to CSV   
            if (usingThreadResultsEnhanced)
            {
                CreateOutputCsvFileForThreadResultsEnhanced(outputFileName);
            }
            else
            {
                CreateOutputCsvFileForThreadResults(outputFileName);                
            }                             

            CreateMalwareFamilyOutputCsvFile(outputFileName, familyMapFileName);
        }


        private void CreateOutputCsvFileForThreadResultsEnhanced(string fileName)
        {
            try
            {
                int correctlyIdentifiedMalwareThreads = 0;
                int incorrectMalwareCount = 0;

                int incorrectNonMalwareCount = 0;
                int correctNonMalwareCount = 0;
                using (var datasetWriter = new StreamWriter(fileName))
                {
                    // Write column names
                    datasetWriter.WriteLine(
                        "Thread ID" + Constants.Delimiter +
                        "Process Name" + Constants.Delimiter +
                        "Is Malware" + Constants.Delimiter +
                        "Total Predictions" + Constants.Delimiter +
                        "Highest Predicted Malware" + Constants.Delimiter +
                        "Highest Predicted Non-Malware" + Constants.Delimiter +
                        "Times Classified as Malware" + Constants.Delimiter +
                        "Times Classified as Non-Malware" + Constants.Delimiter +
                        "Predicted Malware Average" + Constants.Delimiter +
                        "Predicted Non-Malware Average" + Constants.Delimiter +
                        "Classified As Malware?"
                        );

                    foreach (var result in threadResultsMap.Values.OfType<ThreadResultsEnhanced>())
                    {
                        datasetWriter.WriteLine(
                            result.ThreadId + Constants.Delimiter +
                            result.ProcessName + Constants.Delimiter +
                            result.IsMalware + Constants.Delimiter +
                            result.TotalPredictions + Constants.Delimiter +
                            result.HighestMalwarePrediction + Constants.Delimiter +
                            result.HighestNonMalwarePrediction + Constants.Delimiter +
                            result.PredictedMalwareCount + Constants.Delimiter +
                            result.PredictedNonMalwareCount + Constants.Delimiter +
                            result.PredictedMalwareAverage + Constants.Delimiter +
                            result.PredictedNonMalwareAverage + Constants.Delimiter +
                            result.IsClassifiedAsMalware()
                            );


                        if (result.TotalPredictions > 10)
                        {
                            if (result.IsMalware)
                            {
                                if (result.IsClassifiedAsMalware())
                                {
                                    correctlyIdentifiedMalwareThreads++;
                                }
                                else
                                {
                                    incorrectMalwareCount++;
                                }
                            }
                            else  // Handle false positive count
                            {
                                if (result.IsClassifiedAsMalware())
                                {
                                    incorrectNonMalwareCount++;
                                }
                                else
                                {
                                    correctNonMalwareCount++;
                                }
                            }
                        }
                    }
                }

                int totalCount = correctlyIdentifiedMalwareThreads + incorrectMalwareCount;

                double correctPercentage = ((double)correctlyIdentifiedMalwareThreads / (double)totalCount) * 100;
                Console.WriteLine("Correctly identifed malware threads: {0} ({1}%)", correctlyIdentifiedMalwareThreads, correctPercentage);

                double incorrectPercentage = ((double)incorrectMalwareCount / (double)totalCount) * 100;
                Console.WriteLine("Incorrectly identifed malware threads: {0} ({1}%)", incorrectMalwareCount, incorrectPercentage);

                int totalNonMalwareCount = correctNonMalwareCount + incorrectNonMalwareCount;

                double correctPercentageNonMalware = ((double)correctNonMalwareCount / (double)totalNonMalwareCount) * 100;
                Console.WriteLine("Correctly identifed non-malware threads: {0} ({1}%)", correctNonMalwareCount, correctPercentageNonMalware);

                double incorrectPercentageNonMalware = ((double)incorrectNonMalwareCount / (double)totalNonMalwareCount) * 100;
                Console.WriteLine("Incorrectly identifed non-malware threads: {0} ({1}%)", incorrectNonMalwareCount, incorrectPercentageNonMalware);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create CSV output file.  Exception: " + ex.Message);
            }
        }

        private void CreateOutputCsvFileForThreadResults(string fileName)
        {
            try
            {
                int correctlyIdentifiedMalwareThreads = 0;
                int incorrectMalwareCount = 0;

                int incorrectNonMalwareCount = 0;
                int correctNonMalwareCount = 0;
                using (var datasetWriter = new StreamWriter(fileName))
                {
                    // Write column names
                    datasetWriter.WriteLine(
                        "Thread ID" + Constants.Delimiter +
                        "Process Name" + Constants.Delimiter +
                        "Is Malware" + Constants.Delimiter +
                        "Total Context Switches" + Constants.Delimiter +
                        "Correctly Identified Switches" + Constants.Delimiter +
                        "Correctly Identified Percentage" + Constants.Delimiter +
                        "Incorrectly Identified Switches" + Constants.Delimiter +
                        "Incorrectly Identified Percentage" + Constants.Delimiter +
                        "Times Classified As Malware"
                        );

                    foreach (var result in threadResultsMap.Values.OfType<ThreadResults>())
                    {
                        datasetWriter.WriteLine(
                            result.ThreadId + Constants.Delimiter +
                            result.ProcessName + Constants.Delimiter +
                            result.IsMalware + Constants.Delimiter +
                            result.TotalPredictions + Constants.Delimiter +
                            result.ContextSwitchesIdentifiedCorrectly + Constants.Delimiter +
                            result.CorrectPercentage + Constants.Delimiter +
                            result.ContextSwitchesIdentifiedIncorrectly + Constants.Delimiter +
                            result.IncorrectPercentage + Constants.Delimiter +
                            result.ClassifiedAsMalwareCount
                            );


                        if (result.TotalPredictions > 10)
                        {
                            if (result.IsMalware)
                            {
                                if (result.IsClassifiedAsMalware())
                                {
                                    correctlyIdentifiedMalwareThreads++;
                                }
                                else
                                {
                                    incorrectMalwareCount++;
                                }
                            }
                            else  // Handle false positive count
                            {
                                if (result.IsClassifiedAsMalware())
                                {
                                    incorrectNonMalwareCount++;
                                }
                                else
                                {
                                    correctNonMalwareCount++;
                                }
                            }
                        }                      
                    }
                }
            
                int totalCount = correctlyIdentifiedMalwareThreads + incorrectMalwareCount;

                double correctPercentage = ((double)correctlyIdentifiedMalwareThreads / (double)totalCount) * 100;
                Console.WriteLine("Correctly identifed malware threads: {0} ({1}%)", correctlyIdentifiedMalwareThreads, correctPercentage);

                double incorrectPercentage = ((double)incorrectMalwareCount / (double)totalCount) * 100;
                Console.WriteLine("Incorrectly identifed malware threads: {0} ({1}%)", incorrectMalwareCount, incorrectPercentage);

                int totalNonMalwareCount = correctNonMalwareCount + incorrectNonMalwareCount;

                double correctPercentageNonMalware = ((double)correctNonMalwareCount / (double)totalNonMalwareCount) * 100;
                Console.WriteLine("Correctly identifed non-malware threads: {0} ({1}%)", correctNonMalwareCount, correctPercentageNonMalware);

                double incorrectPercentageNonMalware = ((double)incorrectNonMalwareCount / (double)totalNonMalwareCount) * 100;
                Console.WriteLine("Incorrectly identifed non-malware threads: {0} ({1}%)", incorrectNonMalwareCount, incorrectPercentageNonMalware); 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create CSV output file.  Exception: " + ex.Message);
            } 
        }        

        private List<MalwareFamilyInfo> BuildFamilyInfoList(string malwareFamilyMapFileName)
        {
            Dictionary<string, MalwareFamilyInfo> rv = new Dictionary<string,MalwareFamilyInfo>();
            MalwareFamilyMap familyMap = new MalwareFamilyMap(malwareFamilyMapFileName);

            List<string> unmappedProcesses = new List<string>();

            foreach (var result in threadResultsMap.Values)
            {
                // We only care about malware apps for this
                if (result.IsMalware)
                {
                    string familyName = familyMap.GetAssociatedMalwareFamilyName(result.ProcessName);
                    if (!string.IsNullOrWhiteSpace(familyName))
                    {
                        if (rv.ContainsKey(familyName))
                        {
                            rv[familyName].AddThread(result);
                        }
                        else
                        {
                            MalwareFamilyInfo newInfo = new MalwareFamilyInfo(familyName);
                            newInfo.AddThread(result);
                            rv.Add(familyName, newInfo);
                        }
                    }
                    else
                    {
                        unmappedProcesses.Add(result.ProcessName);                        
                    }
                }
            }

            if (unmappedProcesses.Count > 0)
            {
                Console.WriteLine("Warning: Found the following unmapped process names:");

                foreach (string name in unmappedProcesses.Distinct())
                {
                    Console.WriteLine(name);
                }

                Console.WriteLine("\n");
            }

            return rv.Values.ToList();
        }

        private void CreateMalwareFamilyOutputCsvFile(string fileName, string familyMapFileName)
        {
            try
            {
                string directory = Path.GetDirectoryName(fileName);
                string fileNameOnly = Path.GetFileName(fileName);
                string outputFileName = Path.Combine(directory, "malware_families_" + fileNameOnly);

                using (var datasetWriter = new StreamWriter(outputFileName))
                {
                    // Write column names
                    datasetWriter.WriteLine(
                        "Malware Family" + Constants.Delimiter +
                        "Total Testing Threads" + Constants.Delimiter +
                        "Threads Classifies As Malware" + Constants.Delimiter +
                        "Classification Percentage"
                        );

                    double averagePercentSum = 0;
                    var infoList = BuildFamilyInfoList(familyMapFileName);
                    foreach (var info in infoList)
                    {
                        datasetWriter.WriteLine(
                            info.Family + Constants.Delimiter +
                            info.TotalThreads + Constants.Delimiter +
                            info.ThreadsClassifiedAsMalware + Constants.Delimiter +
                            info.ClassificationPercent
                        );

                        averagePercentSum += info.ClassificationPercent;
                    }

                    double averagePercent = averagePercentSum / (double)infoList.Count;
                    Console.WriteLine("Malware Family Average Percentage: {0}", averagePercent);
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create malware family CSV output file.  Exception: " + ex.Message);
            }
        }
    }
}
