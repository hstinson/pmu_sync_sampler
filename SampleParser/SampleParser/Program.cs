using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SampleParser
{
    /// <summary>
    /// Parses the binary samples files in the format of the "Android Malware Performance Counter" data
    /// set from Columbia University.
    /// Link: http://castl.cs.columbia.edu/colmalset/
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser(new SampleAggregator());
         
            if (args.Length < 4)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Example: ./SampleParser <samples directory> <output directory> <Experiment Number (1-N)> <aggregation method> '-includeProcInfo[optional]' \n");
                Console.WriteLine("Aggregation value choices:");
                var choices = parser.GetFeatureVectorDescriptions();
                foreach (string choice in choices)
                {
                    Console.WriteLine(choice);
                }

                return;
            }

            string samplesDir = args[0];
            string outputDir = args[1];
            string experimentNumString = args[2];

            FeatureVectorType aggregationType = FeatureVectorType.SummedContextSwitch;
            if (args.Length >= 4 && !Enum.TryParse<FeatureVectorType>(args[3], out aggregationType))
            {
                Console.WriteLine("Error: Invalid aggregation method choice.  Using default.");
            }

            int experimentNumber = 0;
            if (!int.TryParse(experimentNumString, out experimentNumber))
            {
                Console.WriteLine("Error: Experiment number argument is not a number.");
                return;
            }

            bool includeProcessInfo = false;
            if (args.Length >= 5 && args[4].ToLower().Contains("-include"))
            {
                includeProcessInfo = true;
            }

            bool isTestSet = samplesDir.ToLower().Contains("test");
            parser.CreateDatasetFile(outputDir, aggregationType, includeProcessInfo, experimentNumber, isTestSet);

            // Do pattern matching and build up list of files to parse
            var filesToParse = GetFilesToParse(samplesDir, "*.*");

            Console.WriteLine("\nParsing {0} Files...", filesToParse.Count);

            Parallel.ForEach(filesToParse, fileName =>
            {
                parser.ParseSampleFile(fileName, outputDir, false, includeProcessInfo);
            });

            parser.CloseDataSetFile();
            Console.WriteLine("Parsing complete.");
        }

        private static List<string> GetFilesToParse(string samplesDir, string pattern)
        {
            List<string> rv = null;

            try
            {
                rv = new List<string>(Directory.GetFiles(samplesDir, pattern, SearchOption.TopDirectoryOnly));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to retrieve files list to parse.  Exception: {0}", ex.Message);

                rv = new List<string>();
            }

            return rv;
        }
    }
}
