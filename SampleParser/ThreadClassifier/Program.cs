using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadClassifier
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Thread Classifier -----------------");

            // Args
            /*
             * -Malware package to process name map
             * -Input testing dataset (must include process name and file ID)
             * -Predicted output label file for the input testing dataset
             * -Filename of output file
             */

            /* TASKS
             * 
             * -Map family to process name
             * 
             * -Ensure dataset files have equal amount of vectors
             * -Combine dataset files into single 
             */ 

            /* Outputs
             * 
             * Print to screen (or CSV) the classification results for each thread
             * 
             * 
             * Output 1  - All App Types
             * Total Context Switches Per Thread |  Ctx Switches Identified Correctly |  Ctx Switches Identified Incorrectly | 
             * 
             * 
             * Output 2 - Malware Only
             * Malware family name | Total threads | Threads flagged as malware | Classification Rate 
             * 
             */

            // 
            if (args.Length < 4)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Example: ./ThreadClassifier <malware package mapping file> <test dataset> <output file name> <thread classifcation percentage [1-100]>\n");
                return;
            }

            int classifyPercentage;
            if (!int.TryParse(args[3], out classifyPercentage))
            {
                Console.WriteLine("Error: Invalid thread classify percentage.");
                return;
            }

            ThreadResults.SetClassifcationThresholdPercent(classifyPercentage);

            Console.WriteLine("Parsing dataset...\n");
            DatasetParser parser = new DatasetParser();
            parser.ParseDataset(args[1], args[2], args[0]);

            Console.WriteLine("\nClassification Complete.\n\n");
            //Console.Read();
        }
    }
}
