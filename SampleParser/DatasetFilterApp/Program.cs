using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatasetFilterApp
{
    /// <summary>
    /// Reads a dataset processed by Weka and discards any vectors with a prediction confidence less than
    /// the threshold.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Example: ./DatasetFilterApp <dataset file> <confidence threshold (0.0-1.0)>  <new dataset file>\n");
                return;
            }

            string dataset = args[0];
            string threshString = args[1];
            string newDataset = args[2];

            double threshold;
            if (!double.TryParse(threshString, out threshold) || threshold < 0.0d || threshold > 1.0)
            {
                Console.WriteLine("Error: Confidence threshold must be a number between 0.0 and 1.0.");
            }

            DatasetFilter filter = new DatasetFilter();

            filter.ParseDataset(dataset, threshold, newDataset);

            Console.WriteLine("Done.");
            Console.Read();
        }
    }
}
