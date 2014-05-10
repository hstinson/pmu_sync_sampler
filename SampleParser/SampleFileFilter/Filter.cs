using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SampleFileFilter
{
    public enum FilterType
    {
        MalwareTrain = 0,
        MalwareTest = 1,
        HamTrain = 2,
        HamTest = 3
    }

    public class Filter
    {
        private readonly Dictionary<string, bool> blacklist = new Dictionary<string, bool>();
        private string filteredIndexFile = "filteredIndex.csv";
        private int fileCount = 1;
        private string tracesDirectory;

        private void SetFilteredIndexFileName(FilterType filterType)
        {            
            if (filterType == FilterType.HamTest)
            {
                filteredIndexFile = "ham_test.csv";
            }
            else if (filterType == FilterType.HamTrain)
            {
                filteredIndexFile = "ham_train.csv";
            }
            else if (filterType == FilterType.MalwareTest)
            {
                filteredIndexFile = "malware_test.csv";
            }
            else
            {
                filteredIndexFile = "malware_train.csv";
            }
        }

        private string GetFilteredIndexPrefix(FilterType filterType)
        {
            string rv = null;

            if (filterType == FilterType.HamTest)
            {
                rv = "ham_test_";
            }
            else if (filterType == FilterType.HamTrain)
            {
                rv = "ham_train_";
            }
            else if (filterType == FilterType.MalwareTest)
            {
                rv = "malware_test_";
            }
            else
            {
                rv = "malware_train_";
            }

            return rv;
        }

        public void ReadBlackList(string blackListFile)
        {
            try
            {
                using (StreamReader sr = new StreamReader(blackListFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!blacklist.ContainsKey(line))
                        {
                            blacklist.Add(line, true);
                        }                       
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred reading blacklist. Exception: {0}", ex.Message);
            }
        }

        public void FilterSampleFiles(string sampleFilesDirectory, FilterType filterType)
        {
            // For each folder in sample files dir
            //    Read in index.csv file
            //    For each line in index.csv
            //      if on blacklist, exclude
            //      else, write first two column values to new file and re-name sample file

            if (Directory.Exists(sampleFilesDirectory))
            {
                SetFilteredIndexFileName(filterType);

                var dirs = Directory.EnumerateDirectories(sampleFilesDirectory, "*", SearchOption.TopDirectoryOnly).ToList();

                try
                {
                    // Create traces folder
                    tracesDirectory = Path.Combine(sampleFilesDirectory, "traces");
                    Directory.CreateDirectory(tracesDirectory);

                    if (dirs.Contains(tracesDirectory))
                    {
                        dirs.Remove(tracesDirectory);
                    }

                    // Create new master filter index file
                    using (StreamWriter writer = new StreamWriter(Path.Combine(sampleFilesDirectory, filteredIndexFile)))
                    {
                        // Filter all index files in subdirs
                        foreach (string directory in dirs)
                        {
                            string indexFile = Path.Combine(directory, "index.csv");
                            if (File.Exists(indexFile))
                            {
                                FilterFilesInDirectory(writer, directory, indexFile, filterType);
                            }
                            else
                            {
                                Console.WriteLine("Error: Index.csv file not found in {0}", directory);
                            }
                        }
                    }              
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred filtering sample files. Exception: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Error: Directory not found. {0}", sampleFilesDirectory);
            }
        }

        private void FilterFilesInDirectory(StreamWriter masterIndexFileStream, string directory, string indexFilePath, FilterType filterType)
        {
            //    Read in index.csv file
            //    For each line in index.csv
            //      if on blacklist, exclude
            //      else, write first two column values to new file and re-name sample file
            const string delimiter = ",";

            try
            {
                using (StreamReader sr = new StreamReader(indexFilePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] items = line.Split(',');
                        if (items.Length == 4)
                        {
                            if (!blacklist.ContainsKey(items[1]))
                            {
                                string newSampleFileName = GetFilteredIndexPrefix(filterType) + fileCount + ".bin";
                                string newSampleFilePath = "traces/" + newSampleFileName;

                                // Move file to new folder
                                int indexOfLastSlash = items[3].LastIndexOf("/");
                                string sourceFileName = items[3].Substring(indexOfLastSlash+1);

                                File.Copy(Path.Combine(directory, sourceFileName), Path.Combine(tracesDirectory, newSampleFileName));

                                string lineToWrite = items[0] + delimiter +
                                                     items[1] + delimiter +
                                                     items[2] + delimiter +
                                                     newSampleFilePath;

                                masterIndexFileStream.WriteLine(lineToWrite);

                                fileCount++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Warning: Skipped line in index.csv file");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred filtering index file {0}. Exception: {1}", indexFilePath, ex.Message);
            }
        }
    }
}
