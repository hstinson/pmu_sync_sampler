using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleFileFilter
{
    public class Program
    {
        static void Main(string[] args)
        {
            Filter filter = new Filter();          
            Console.WriteLine("Filtering sample files...");

            // Get blacklist file
            // Get parent directory
            // Get filter type
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Example: ./SampleFileFilter <parent directory> <filter type (0-3)> <blacklist file [Optional]>\n");
                Console.WriteLine("Filter Types:\nMalwareTrain = 0\nMalwareTest = 1\nHamTrain = 2\nHamTest = 3\n\n");
                return;
            }

            int filterType;
            if (!int.TryParse(args[1], out filterType) || filterType < 0 || filterType > 3)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine("Filter type not a value between 0 and 3.");
                return;
            }

            if (args.Length >= 3)
            {
                filter.ReadBlackList(args[2]);
            }

            FilterType filterTypeEnum = (FilterType)filterType;
            filter.FilterSampleFiles(args[0], filterTypeEnum);

            Console.WriteLine("\nFiltering Complete.  Press any key to exit.\n\n");
            Console.Read();
        }
    }
}
