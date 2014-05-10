# Script to perform thread classification
#
# 
runThreadClassifier () #Param - output dir, test dataset
{
	OutputDir="$1"
	TestDataset="$2"
	ClassifierFileName="$OutputDir/ThreadClassificationResults.csv"
	FamilyFile="$OutputDir/malware_families_threadClassifyResults.csv"
	
	if [ -a "$ClassifierFileName" ]; then
		rm "$ClassifierFileName"
	fi
	
	if [ -a "$FamilyFile" ]; then
		rm "$FamilyFile"
	fi
	
	#Run the classifier
	echo .
	echo "Thread classification for $TestDataset...."
	echo .
	../SampleParser/ThreadClassifier/bin/Debug/ThreadClassifier.exe malware_family_map.csv "$TestDataset" "$ClassifierFileName" 5
}

# perform for all experiments
echo Perfoming HISTOGRAM Based Classification
runThreadClassifier "Experiment_1/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_1/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_2/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_2/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_3/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_3/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_4/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_4/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_5/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_5/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_6/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_6/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

runThreadClassifier "Experiment_7/WekaClassification_HistogramCtx_OutlierSkip/" "Experiment_7/WekaClassification_HistogramCtx_OutlierSkip/test_procInfo_randomForest_histogram_predictions.arff"

echo .
echo .
echo . 
echo Perfoming HISTOGRAM Based Classification

runThreadClassifier "Experiment_1/WekaClassification_SumContextSwitch/" "Experiment_1/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_2/WekaClassification_SumContextSwitch/" "Experiment_2/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_3/WekaClassification_SumContextSwitch/" "Experiment_3/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_4/WekaClassification_SumContextSwitch/" "Experiment_4/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_5/WekaClassification_SumContextSwitch/" "Experiment_5/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_6/WekaClassification_SumContextSwitch/" "Experiment_6/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"

runThreadClassifier "Experiment_7/WekaClassification_SumContextSwitch/" "Experiment_7/WekaClassification_SumContextSwitch/test_procInfo_randomForest_contextSwitch_predictions.arff"