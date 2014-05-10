# Script to predict, combine dataset, and perform thread classification
#
# 
test $# -lt 3 && echo "runClassification.sh <machine learning file> <test dataset> <output directory>" >& 2 && exit
LearningAlgFile=$1
TestDataSet=$2
OutputDir=$3

mkdir $OutputDir

PredictedFileName="$OutputDir/predictedLabels.arff"

#Run the prediction
# Ignore columns 6 and 7 as they are the process name and thread ID
echo "Predicting...."
waffles_learn.exe predict -seed 1 "$LearningAlgFile" "$TestDataSet" -ignore 6,7 > "$PredictedFileName"

#Combine the datasets
echo "Merging datasets...."

#First rename the label in the predicted file; All atrtibutes/labels must be unique or Weka merge will not work
perl -pi -e 's/class/predicted_class/g' "$PredictedFileName"

CombinedDatasetFileName="$OutputDir/combinedTestDataset.arff"
java -Xmx1024m weka.core.Instances merge "$TestDataSet" "$PredictedFileName" > "$CombinedDatasetFileName"

#Run the classifier
echo "Thread classification...."
ClassifierFileName="$OutputDir/ThreadClassificationResults.csv"
./ThreadClassifier.exe malware_family_map.csv "$CombinedDatasetFileName" "$ClassifierFileName" 5

echo "Done.  Output written to $OutputDir"