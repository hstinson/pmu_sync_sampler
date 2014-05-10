using System;
namespace SampleParser
{
    public interface ISampleAggregator
    {
        void AggregateData(System.Collections.Generic.List<Sample> samples, FeatureVectorType aggregateType, bool includeProcessInfo = false);
        event Action<string> DataAggregatedEvent;
        event Action<System.Collections.Generic.List<string>> MultipleDataAggregatedEvent;
    }
}
