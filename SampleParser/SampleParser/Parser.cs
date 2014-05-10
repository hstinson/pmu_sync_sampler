using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace SampleParser
{
    public enum FeatureVectorType
    {
        [Description("1.) Raw samples")]
        RawSamples = 1,

        [Description("2.) Sum values between context switch")]
        SummedContextSwitch = 2,

        [Description("3.) Average values between context switch")]
        AverageContextSwitch = 3,

        [Description("4.) Summed values between context switch with additional dimension for total samples")]
        SummedContextSwitchWithTotalCountIncluded = 4,

        [Description("5.) Average values between context switch with additional dimension for total samples")]
        AverageContextSwithWithTotalCountIncluded = 5,

        [Description("6.) Histograms without regard to context switches")]
        Histogram = 6,

        [Description("7.) Histograms between context switches")]
        HistogramBetweenContextSwitches = 7
    }

    public class Parser
    {        
        private StreamWriter datasetWriter;
        private FeatureVectorType aggregateType;
        private readonly ISampleAggregator sampleAggregator;
        private long sampleFileIdCounter = 0;

        public Parser(ISampleAggregator sampleAggregatorArg)
        {
            sampleAggregator = sampleAggregatorArg;
        }

        public List<string> GetFeatureVectorDescriptions()
        {
            var rv = new List<string>();

            foreach (Enum value in Enum.GetValues(typeof(FeatureVectorType)))
            {
                FieldInfo fi = value.GetType().GetField(value.ToString());

                DescriptionAttribute[] attributes =
                        (DescriptionAttribute[])fi.GetCustomAttributes(
                        typeof(DescriptionAttribute),
                        false);

                if (attributes != null &&
                    attributes.Length > 0)
                {
                    rv.Add(attributes[0].Description);
                } 
            }

            return rv;
        }

        private void WriteToDataSet(string lineToWrite)
        {
            lock (datasetWriter)
            {
                datasetWriter.WriteLine(lineToWrite);
            }
        }

        private void WriteToDataSet(List<string> linesToWrite)
        {
            lock (datasetWriter)
            {
                foreach (string line in linesToWrite)
                {
                    datasetWriter.WriteLine(linesToWrite);
                }
            }
        }

        private string GenerateOutputFileName(FeatureVectorType aggregateTypeArg, bool includeProcessInfo, bool isTestSet)
        {
            string rv = null;
            string testType = (isTestSet) ? "test" : "train";
            string procinfoString = (includeProcessInfo) ? "procInfo_" : "";

            string histogramInformation = "";
            if (aggregateTypeArg == FeatureVectorType.HistogramBetweenContextSwitches)
            {
                histogramInformation = "_CtxSwitch";
            }
            else if (aggregateTypeArg == FeatureVectorType.Histogram)
            {
                histogramInformation = "_IgnoreCtxSwitch";
            }

            rv = string.Format("{0}_{1}dataset_{2}{3}.arff", testType, procinfoString, aggregateType.ToString(), histogramInformation);

            return rv;
        }


        /// <summary>
        /// Creates a dataset file that logs the summed values of a sample file.
        /// </summary>
        /// <param name="outputDirectory">Directory where the dataset file will be saved.</param>
        public void CreateDatasetFile(
            string outputDirectory, 
            FeatureVectorType aggregateTypeArg, 
            bool includeProcessInfo, 
            int experimentNum, 
            bool isTestSet)
        {
            if (datasetWriter != null)
            {
                datasetWriter.Close();
                datasetWriter.Dispose();
                datasetWriter = null;

                sampleFileIdCounter = 0;
            }

            aggregateType = aggregateTypeArg;
            string outputFileName = GenerateOutputFileName(aggregateType, includeProcessInfo, isTestSet);
            string outputFile = Path.Combine(outputDirectory, outputFileName);

            try
            {
                FileStream stream = new FileStream(outputFile, FileMode.Append);
                datasetWriter = new StreamWriter(stream);

                if (stream.Position == 0)
                {
                    // We are creating a new file, so format the top of the file into an ARFF format
                    
                    if (aggregateType == FeatureVectorType.Histogram || aggregateType == FeatureVectorType.HistogramBetweenContextSwitches)
                    {
                        datasetWriter.WriteLine("@RELATION " + "Experiment_" + experimentNum + "_" + outputFileName);
                        WriteHistogramAttributes();
                    }
                    else
                    {
                        datasetWriter.WriteLine("@RELATION " + outputFileName);
                        WritePmuAttributeInformation(experimentNum);
                    }                   

                    if (aggregateType == FeatureVectorType.AverageContextSwithWithTotalCountIncluded ||
                        aggregateType == FeatureVectorType.SummedContextSwitchWithTotalCountIncluded)
                    {
                        datasetWriter.WriteLine("@ATTRIBUTE ValBetweenContextSwitch real");
                    }

                    if (includeProcessInfo)
                    {
                        datasetWriter.WriteLine("@ATTRIBUTE Process_Name string");
                        datasetWriter.WriteLine("@ATTRIBUTE Sample_File_ID real");
                    }

                    datasetWriter.WriteLine(string.Format("@ATTRIBUTE class {{{0},{1}}}", Constants.MalwareString, Constants.NonMalwareString));
                    datasetWriter.WriteLine("");
                    datasetWriter.WriteLine("@DATA");
                }

                sampleAggregator.DataAggregatedEvent += WriteToDataSet;
                sampleAggregator.MultipleDataAggregatedEvent += WriteToDataSet;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create dataset output file.  Exception: " + ex.Message);
            }            
        }

        private void WriteHistogramAttributes()
        {
            const string EventText = "Event_";

            for (int i = 1; i <= 6; i++)
            {
                datasetWriter.WriteLine(string.Format("@ATTRIBUTE {0}{1}_BinSize real", EventText, i));
                datasetWriter.WriteLine(string.Format("@ATTRIBUTE {0}{1}_Min real", EventText, i));
                datasetWriter.WriteLine(string.Format("@ATTRIBUTE {0}{1}_Max real", EventText, i));

                for (int k = 1; k <= Constants.BucketCountHistogram; k++)
                {
                    datasetWriter.WriteLine(string.Format("@ATTRIBUTE {0}{1}_Bin_{2} real", EventText, i, k));
                }
            }
        }

        private void WritePmuAttributeInformation(int experimentNumber)
        {
            if (experimentNumber == 1)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Memory_Reads real");
                datasetWriter.WriteLine("@ATTRIBUTE Memory_Writes real");
                datasetWriter.WriteLine("@ATTRIBUTE PC_Change real");
                datasetWriter.WriteLine("@ATTRIBUTE Immediate_Branch real");
                datasetWriter.WriteLine("@ATTRIBUTE Unaligned_Access real");
                datasetWriter.WriteLine("@ATTRIBUTE Unpredicted_Branch real");
            }
            else if (experimentNumber == 2)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Branch_Mispredicts real");
                datasetWriter.WriteLine("@ATTRIBUTE Instruction_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Cache_Access real");
                datasetWriter.WriteLine("@ATTRIBUTE Predictable_Func_Returns real");
                datasetWriter.WriteLine("@ATTRIBUTE Instruction_Cache_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Cache_Miss real");
            }
            else if (experimentNumber == 3)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Data_Micro_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE STREX_Passed real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Eviction real");
                datasetWriter.WriteLine("@ATTRIBUTE DSB_Instructions real");
                datasetWriter.WriteLine("@ATTRIBUTE Coherent_Linefill_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Coherent_Linefill_Hit real");
            }
            else if (experimentNumber == 4)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Issue_Dispatch_No_Instruc real");
                datasetWriter.WriteLine("@ATTRIBUTE Issue_Empty real");
                datasetWriter.WriteLine("@ATTRIBUTE Instruc_Core_Name_Stage real");
                datasetWriter.WriteLine("@ATTRIBUTE Main_Exec_Unit_Instrucs real");
                datasetWriter.WriteLine("@ATTRIBUTE Second_Exec_Unit_Instrucs real");
                datasetWriter.WriteLine("@ATTRIBUTE Load_Store_Instructions real");
            }
            else if (experimentNumber == 5)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Proc_Stall_Mem_Write real");
                datasetWriter.WriteLine("@ATTRIBUTE Proc_Stall_Ins_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Proc_Stall_Data_Side_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Proc_Stall_Ins_Micro_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Proc_Stall_Data_Micro_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Integer_Clock_Enabled real");
            }
            else if (experimentNumber == 6)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Memory_Reads real");
                datasetWriter.WriteLine("@ATTRIBUTE Memory_Writes real");
                datasetWriter.WriteLine("@ATTRIBUTE PC_Change real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Cache_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Predictable_Func_Returns real");
                datasetWriter.WriteLine("@ATTRIBUTE Instruction_TLB_Miss real");               
            }
            else if (experimentNumber == 7)
            {
                datasetWriter.WriteLine("@ATTRIBUTE Memory_Reads real");
                datasetWriter.WriteLine("@ATTRIBUTE PC_Change real");
                datasetWriter.WriteLine("@ATTRIBUTE Instruction_TLB_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Cache_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Coherent_Linefill_Miss real");
                datasetWriter.WriteLine("@ATTRIBUTE Data_Eviction real");
            }
            else
            {
                datasetWriter.WriteLine("@ATTRIBUTE Event1 real");
                datasetWriter.WriteLine("@ATTRIBUTE Event2 real");
                datasetWriter.WriteLine("@ATTRIBUTE Event3 real");
                datasetWriter.WriteLine("@ATTRIBUTE Event4 real");
                datasetWriter.WriteLine("@ATTRIBUTE Event5 real");
                datasetWriter.WriteLine("@ATTRIBUTE Event6 real");
            }
        }

        /// <summary>
        /// Closes the dataset writer stream.
        /// </summary>
        public void CloseDataSetFile()
        {
            if (datasetWriter != null)
            {
                datasetWriter.Flush();
                datasetWriter.Close();
                datasetWriter.Dispose();

                sampleAggregator.DataAggregatedEvent -= WriteToDataSet;
                sampleAggregator.MultipleDataAggregatedEvent -= WriteToDataSet;
            }
        }

        /// <summary>
        /// Parses a sample file and writes its' output to a CSV file of the same file name.
        /// </summary>
        /// <param name="fileName">File name (includes file path) of the file to parse.</param>
        /// <param name="outputDirectory">Directory where the CSV file will be saved.</param>
        /// <param name="createCsvFile">Specifies whether or not to create a CSV file with the values parsed.</param>
        /// <param name="includeProcessInfo">Include process info and sample file ID in the dataset for each vector.</param>
        public void ParseSampleFile(string fileName, string outputDirectory, bool createCsvFile = true, bool includeProcessInfo = false)
        {
            List<Sample> samples = new List<Sample>();

            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open), Encoding.ASCII))
            {
                // There exists three C-style strings at the beginning of each file
                string s1 = ReadCString(reader);
                string processName = ReadCString(reader);
                string s3 = ReadCString(reader);

                long threadId = GetThreadId();

                // Position and length variables
                int pos = s1.Length + processName.Length + s3.Length;
                int length = (int)reader.BaseStream.Length;
                while (pos < length)
                {
                    Sample sample = new Sample();

                    sample.CycleCount = reader.ReadUInt32();
                    sample.EventValue1 = reader.ReadUInt32();
                    sample.EventValue2 = reader.ReadUInt32();
                    sample.EventValue3 = reader.ReadUInt32();
                    sample.EventValue4 = reader.ReadUInt32();
                    sample.EventValue5 = reader.ReadUInt32();
                    sample.EventValue6 = reader.ReadUInt32();

                    sample.ProcessName = processName.TrimEnd('\0');
                    sample.SampleFileId = threadId;

                    string fileNameOnly = Path.GetFileName(fileName);
                    sample.FromMalware = fileNameOnly.Contains(Constants.MalwareString);

                    pos += sizeof(uint) * 7;

                    samples.Add(sample);
                }
            }

            // If there does not exist a context switch sample at the end of the list, add one so the dataset aggregation
            // methods behave as expected.
            if (!samples.Last().IsContextSwitch())
            {
                samples.Add(new Sample());
            }

            if (datasetWriter != null)
            {
                sampleAggregator.AggregateData(samples, aggregateType, includeProcessInfo);
            }

            if (createCsvFile)
            {
                string newFileNameWithOldPath = Path.ChangeExtension(fileName, ".csv");
                string newFilePath = Path.Combine(outputDirectory, Path.GetFileName(newFileNameWithOldPath));
                OutputToCsvFile(newFilePath, samples);   
            }
        }

        private long GetThreadId()
        {
            return Interlocked.Increment(ref sampleFileIdCounter);
        }

        private string ReadCString(BinaryReader reader)
        {
            List<char> charlist = new List<char>();
            char c;
            do
            {
                c = reader.ReadChar();
                charlist.Add(c);
            } while (c != '\0');

            string rv = new string(charlist.ToArray());
            return rv;
        }

        private void OutputToCsvFile(string fileName, List<Sample> samples)
        {
            try
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Create))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    foreach (Sample sample in samples)
                    {
                        string sampleString =
                            sample.CycleCount + Constants.Delimiter +
                            sample.EventValue1 + Constants.Delimiter +
                            sample.EventValue2 + Constants.Delimiter +
                            sample.EventValue3 + Constants.Delimiter +
                            sample.EventValue4 + Constants.Delimiter +
                            sample.EventValue5 + Constants.Delimiter +
                            sample.EventValue6;

                        writer.WriteLine(sampleString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing samples to {0}.   Exception: {1}", fileName, ex.Message);
            }
        }
    }
}
