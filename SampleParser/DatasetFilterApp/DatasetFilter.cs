using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DatasetFilterApp
{
    public class DatasetFilter
    {
        private const string Delimiter = ",";

        public void ParseDataset(string datasetFile, double threshold, string newDatasetFileName)
        {
            using (var datasetReader = new StreamReader(datasetFile))
            using (var datasetWriter = new StreamWriter(newDatasetFileName))
            {
                // skip past inital items in ARFF file
                int count = 0;
                string dummy;
                while ((dummy = datasetReader.ReadLine()) != "@data")
                {
                    datasetWriter.WriteLine(dummy);

                    if (count > 150)
                    {
                        // data string not found, exit out of loop, file is bad
                        break;
                    }
                    count++;
                }

                datasetWriter.WriteLine("@data");

                while (!datasetReader.EndOfStream)
                {
                    string line = datasetReader.ReadLine();
                    var splits = line.Split(',');
                    if (splits.Length >= 10)  // 6 PMU events, correct classification, predicted classification, malware probabilty, non-malware probability
                    {
                        string correctClass = splits[splits.Length - 4];
                        string predictedClass = splits[splits.Length - 3];
                        string malPredictString = splits[splits.Length - 2];
                        string nonMalPredictString = splits[splits.Length - 1];

                        double malwarePrediction;
                        double nonMalwarePrediction;

                        bool writeLine = false;

                        if (double.TryParse(malPredictString, out malwarePrediction) &&
                            double.TryParse(nonMalPredictString, out nonMalwarePrediction))
                        {
                            if (Equals(correctClass, predictedClass))
                            {
                                if (Equals("malware", correctClass) && malwarePrediction >= threshold)
                                {
                                    writeLine = true;
                                }
                                else if (Equals("non_malware", correctClass))
                                {
                                    writeLine = true;
                                }

                                if (writeLine)
                                {
                                    datasetWriter.WriteLine(line);
                                }                                
                            }
                        }
                        else
                        {
                            Console.WriteLine("Warning: Encountered line in dataset file that does not contain decimal prediction values.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Warning: Encountered line in dataset file that does not contain the correct amount of attributes and labels.");
                    }
                }
            }

            // Done reading dataset file, 
            
        }
    }
}
