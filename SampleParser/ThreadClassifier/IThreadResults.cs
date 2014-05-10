using System;
namespace ThreadClassifier
{
    public interface IThreadResults
    {
        bool IsMalware { get; }
        string ProcessName { get; }
        int ThreadId { get; }
        int TotalPredictions { get; }

        void AddPredictionInfo(string correctLabel, string predictedLabel, double predictedMalware = -1.0d, double predictedNonMalware = -1.0d);
        bool IsClassifiedAsMalware();
    }
}
