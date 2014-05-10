using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SampleParser.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class SampleAggregatorTests
    {
        public class AggregatedSample
        {
            public uint EventValue1 = 0;
            public uint EventValue2 = 0;
            public uint EventValue3 = 0;
            public uint EventValue4 = 0;
            public uint EventValue5 = 0;
            public uint EventValue6 = 0;
            public uint SampleCountBetweenContextSwitches = 0;
            public string MalwareString;
        }

        public SampleAggregatorTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private List<Sample> GenerateSamples(uint count, uint value, uint dataContextSwitchModulo)
        {
            List<Sample> samples = new List<Sample>();

            for (int i = 1; i <= count; i++)
            {
                if (i % dataContextSwitchModulo == 0)
                {
                    samples.Add(new Sample());  // Simulates a context switch
                }
                else
                {
                    samples.Add(
                        new Sample()
                        {
                            CycleCount = 0, // not caring about cycle count at the moment so leave at zero
                            EventValue1 = value,
                            EventValue2 = value,
                            EventValue3 = value,
                            EventValue4 = value,
                            EventValue5 = value,
                            EventValue6 = value,
                            FromMalware = true
                        });
                }
            }

            return samples;
        }

        private List<Sample> GenerateDifferentSamples(uint count, uint value, uint dataContextSwitchModulo)
        {
            List<Sample> samples = new List<Sample>();

            for (uint i = 1; i <= count; i++)
            {
                if (i % dataContextSwitchModulo == 0)
                {
                    samples.Add(new Sample());  // Simulates a context switch
                }
                else
                {
                    samples.Add(
                        new Sample()
                        {
                            CycleCount = 0, // not caring about cycle count at the moment so leave at zero
                            EventValue1 = value * i,
                            EventValue2 = value * i,
                            EventValue3 = value * i,
                            EventValue4 = value * i,
                            EventValue5 = value * i,
                            EventValue6 = value * i,
                            FromMalware = true
                        });
                }
            }

            return samples;
        }

        private AggregatedSample ConvertToSample(string sampleString)
        {
            AggregatedSample sample = new AggregatedSample();

            string[] items = sampleString.Split(',');
            if (items.Length == 7)
            {
                sample.EventValue1 = uint.Parse(items[0]);
                sample.EventValue2 = uint.Parse(items[1]);
                sample.EventValue3 = uint.Parse(items[2]);
                sample.EventValue4 = uint.Parse(items[3]);
                sample.EventValue5 = uint.Parse(items[4]);
                sample.EventValue6 = uint.Parse(items[5]);
                sample.MalwareString = items[6];
            }
            else if (items.Length == 8)
            {
                sample.EventValue1 = uint.Parse(items[0]);
                sample.EventValue2 = uint.Parse(items[1]);
                sample.EventValue3 = uint.Parse(items[2]);
                sample.EventValue4 = uint.Parse(items[3]);
                sample.EventValue5 = uint.Parse(items[4]);
                sample.EventValue6 = uint.Parse(items[5]);
                sample.SampleCountBetweenContextSwitches = uint.Parse(items[6]);
                sample.MalwareString = items[7];
            }
            else
            {
                Assert.Fail("Sample not of correct size.");
            }

            return sample;
        }

        [TestMethod]
        public void Test_VerifyBinningWorks()
        {
            SampleAggregator uut = new SampleAggregator();

            uint count = 1000;
            uint value = 10;
            var samples = GenerateDifferentSamples(count, value, 50);

            List<string> aggregatedSamples = new List<string>();
            uut.DataAggregatedEvent +=
                sampleString =>
                {
                    aggregatedSamples.Add(sampleString);
                };

            uut.AggregateData(samples, FeatureVectorType.Histogram);
        }

        [TestMethod]
        public void Test_VerifySumBetweenDataContextSwitch()
        {
            SampleAggregator uut = new SampleAggregator();

            uint count = 100;
            uint value = 10;

            var samples = GenerateSamples(count, value, value);

            List<AggregatedSample> aggregatedSamples = new List<AggregatedSample>();
            uut.DataAggregatedEvent +=
                sampleString =>
                {                    
                    aggregatedSamples.Add(ConvertToSample(sampleString));
                };

            uut.AggregateData(samples, FeatureVectorType.SummedContextSwitch);


            Assert.AreEqual<int>((int)(count / value), aggregatedSamples.Count);
            uint expectedValue = value * (value - 1);
            foreach (AggregatedSample s in aggregatedSamples)
            {
                Assert.AreEqual<uint>(expectedValue, s.EventValue1);
                Assert.AreEqual<uint>(expectedValue, s.EventValue2);
                Assert.AreEqual<uint>(expectedValue, s.EventValue3);
                Assert.AreEqual<uint>(expectedValue, s.EventValue4);
                Assert.AreEqual<uint>(expectedValue, s.EventValue5);
                Assert.AreEqual<uint>(expectedValue, s.EventValue6);
            }
        }

        [TestMethod]
        public void Test_VerifySumBetweenDataContextSwitchWithSampleCountDimension()
        {
            SampleAggregator uut = new SampleAggregator();

            uint count = 520;
            uint value = 10;

            var samples = GenerateSamples(count, value, value);

            List<AggregatedSample> aggregatedSamples = new List<AggregatedSample>();
            uut.DataAggregatedEvent +=
                sampleString =>
                {
                    aggregatedSamples.Add(ConvertToSample(sampleString));
                };

            uut.AggregateData(samples, FeatureVectorType.SummedContextSwitchWithTotalCountIncluded);


            Assert.AreEqual<int>((int)(count / value), aggregatedSamples.Count);
            uint expectedValue = value * (value - 1);
            foreach (AggregatedSample s in aggregatedSamples)
            {
                Assert.AreEqual<uint>(expectedValue, s.EventValue1);
                Assert.AreEqual<uint>(expectedValue, s.EventValue2);
                Assert.AreEqual<uint>(expectedValue, s.EventValue3);
                Assert.AreEqual<uint>(expectedValue, s.EventValue4);
                Assert.AreEqual<uint>(expectedValue, s.EventValue5);
                Assert.AreEqual<uint>(expectedValue, s.EventValue6);
                Assert.AreEqual<uint>(value - 1, s.SampleCountBetweenContextSwitches);
            }
        }


        [TestMethod]
        public void Test_VerifyAverageBetweenDataContextSwitch()
        {
            SampleAggregator uut = new SampleAggregator();

            uint count = 200;
            uint value = 10;

            var samples = GenerateSamples(count, value, value);

            List<AggregatedSample> aggregatedSamples = new List<AggregatedSample>();
            uut.DataAggregatedEvent +=
                sampleString =>
                {
                    aggregatedSamples.Add(ConvertToSample(sampleString));
                };

            uut.AggregateData(samples, FeatureVectorType.AverageContextSwitch);


            Assert.AreEqual<int>((int)(count / value), aggregatedSamples.Count);
            uint expectedValue = (value * (value - 1)) / (value - 1);  // 
            foreach (AggregatedSample s in aggregatedSamples)
            {
                Assert.AreEqual<uint>(expectedValue, s.EventValue1);
                Assert.AreEqual<uint>(expectedValue, s.EventValue2);
                Assert.AreEqual<uint>(expectedValue, s.EventValue3);
                Assert.AreEqual<uint>(expectedValue, s.EventValue4);
                Assert.AreEqual<uint>(expectedValue, s.EventValue5);
                Assert.AreEqual<uint>(expectedValue, s.EventValue6);
            }
        }
    }
}
