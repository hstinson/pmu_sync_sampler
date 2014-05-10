using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using SampleParser;
using System.Windows.Input;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SampleParserApp
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly Parser parser = new Parser(new SampleAggregator());

        #region Data-Bound Elements

        private string samplesDirectory;
        /// <summary>
        /// Samples directory.
        /// </summary>
        public string SamplesDirectory
        {
            get { return samplesDirectory; }
            set 
            {
                samplesDirectory = value; 
                OnPropertyChanged("SamplesDirectory");
            }
        }

        private string outputDirectory;
        /// <summary>
        /// Output directory.
        /// </summary>
        public string OutputDirectory
        {
            get { return outputDirectory; }
            set
            {
                outputDirectory = value;
                OnPropertyChanged("OutputDirectory");
            }
        }

        private string samplesPattern = "*.*";
        /// <summary>
        /// Pattern used to match the files to parse in the dataset.
        /// </summary>
        public string SamplesPattern
        {
            get { return samplesPattern; }
            set
            {
                samplesPattern = value;
                OnPropertyChanged("SamplesPattern");
            }
        }

        private bool createCsvFiles;
        /// <summary>
        /// Specifies whether or not to create CSV files from the parsed raw sample files.
        /// </summary>
        public bool CreateCsvFiles
        {
            get { return createCsvFiles; }
            set 
            { 
                createCsvFiles = value; 
                OnPropertyChanged("CreateCsvFiles"); 
            }
        }

        private List<string> aggregationMethods = new List<string>();
        public List<string> AggregationMethods
        {
            get
            {
                return aggregationMethods;
            }
        }

        private FeatureVectorType aggregationType = FeatureVectorType.SummedContextSwitch;
        /// <summary>
        /// Pattern used to match the files to parse in the dataset.
        /// </summary>
        public string AggregationType
        {
            get { return aggregationType.ToString(); }
            set
            {
                aggregationType = (FeatureVectorType)Enum.Parse(typeof(FeatureVectorType), value);
                OnPropertyChanged("AggregationType");
            }
        }

        private bool createDatasetButtonEnabled = true;
        /// <summary>
        /// Specifies whether or not the create dataset button is enabled.
        /// </summary>
        public bool CreateDatasetButtonEnabled
        {
            get { return createDatasetButtonEnabled; }
            set
            {
                createDatasetButtonEnabled = value;
                OnPropertyChanged("CreateDatasetButtonEnabled");
            }
        }

        private bool includeProcessInformation = true;
        /// <summary>
        /// Specifies whether or not to include process name and sample file ID in the dataset.
        /// </summary>
        public bool IncludeProcessInformation
        {
            get { return includeProcessInformation; }
            set
            {
                includeProcessInformation = value;
                OnPropertyChanged("IncludeProcessInformation");
            }
        }

        private string outputText = "";
        /// <summary>
        /// Output text.
        /// </summary>
        public string OutputText
        {
            get { return outputText; }
            set
            {
                outputText = value;
                OnPropertyChanged("OutputText");
            }
        }

        private int experimentNumber = 1;
        /// <summary>
        /// Experiment number.
        /// </summary>
        public int ExperimentNumber
        {
            get
            {
                return experimentNumber;
            }
            set
            {
                experimentNumber = value;
                OnPropertyChanged("ExperimentNumber");
            }
        }

        public ICommand SelectSampleDirectoryCommand { get; private set; }
        public ICommand SelectOutputDirectoryCommand { get; private set; }
        public ICommand CreateDatasetCommand { get; private set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private Dispatcher dispatcher;

        public MainWindowViewModel(Dispatcher dispatcherArg)
        {
            dispatcher = dispatcherArg;
            aggregationMethods.AddRange(Enum.GetNames(typeof(FeatureVectorType)));

            SelectSampleDirectoryCommand = new RelayCommand(_ => ShowSamplesDirectoryBrowser());
            SelectOutputDirectoryCommand = new RelayCommand(_ => ShowOutputDirectoryBrowser());
            CreateDatasetCommand = new RelayCommand(_ => CreateDataset());
        }

        private void ShowSamplesDirectoryBrowser()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select Samples Directory";
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SamplesDirectory = dialog.SelectedPath;
            }
        }

        private void ShowOutputDirectoryBrowser()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select Output Directory";
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputDirectory = dialog.SelectedPath;
            }
        }

        private void OnPropertyChanged(string prop)
        {
           if (PropertyChanged != null )
           {
              PropertyChanged(this, new PropertyChangedEventArgs(prop));
           }
        }

        private void CreateDataset()
        {
            // Do pattern matching and build up list of files to parse
            var filesToParse = GetFilesToParse(samplesDirectory, SamplesPattern);

            CreateDatasetButtonEnabled = false;
            OutputText = string.Format("Parsing {0} Files...", filesToParse.Count);

            Task.Factory.StartNew(() =>
                {
                    bool isTestSet = samplesDirectory.ToLower().Contains("test");
                    parser.CreateDatasetFile(outputDirectory, aggregationType, includeProcessInformation, ExperimentNumber, isTestSet);                    

                    Parallel.ForEach(filesToParse, fileName =>
                    {
                        parser.ParseSampleFile(fileName, outputDirectory, createCsvFiles, includeProcessInformation);
                    });

                    parser.CloseDataSetFile();
                })
                .ContinueWith(_ =>
                {
                    dispatcher.Invoke(new Action(() => 
                    {
                        CreateDatasetButtonEnabled = true;
                        OutputText = "Parsing Complete!";
                        MessageBox.Show(string.Format("Dataset file written to {0}", outputDirectory), "Parsing Complete");
                    }));                    
                });           
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
                string message = string.Format("Unable to retrieve files list to parse.  Exception: {0}", ex.Message);
                Console.WriteLine(message);
                MessageBox.Show(message);

                rv = new List<string>();
            }

            return rv;
        }
    }
}
