
test $# -lt 3 && echo "runExperimentClassifiers.sh <directory of datasets> <training dataset> <testing dataset> " >& 2 && exit
DatasetDir=$1

#"test_dataset_SummedContextSwitch.arff"
#"train_dataset_SummedContextSwitch.arff"

TrainSetName=$2
TestSetName=$3

cd "$DatasetDir"

echo -e "---------------\n"
echo -e "Basic stats...\n"
echo -e "---------------\n"

echo -e "\nStats for training set:\n"
waffles_plot.exe stats $TrainSetName

echo -e "\nStats for testing set:\n"
waffles_plot.exe stats $TestSetName

echo -e "\n\n---------------\n"
echo -e "Combine the test/training set\n"
echo -e "---------------\n"

CombinedDatasetFile="combinedDataset.arff"
waffles_transform.exe mergevert $TrainSetName $TestSetName > $CombinedDatasetFile

echo -e "\n\n---------------\n"
echo -e "Salience testing...\n"
echo -e "---------------\n"

echo -e "\nTraining Set:\n"
waffles_dimred.exe attributeselector $TrainSetName

echo -e "\nTesting Set:\n"
waffles_dimred.exe attributeselector $TestSetName

echo -e "\nCombined Set:\n"
waffles_dimred.exe attributeselector $CombinedDatasetFile

echo -e "\n\n---------------\n"
echo -e "Context-switch based training and testing\n"
echo -e "---------------\n"

echo -e "\nNaive bayes:\n"
waffles_learn.exe train -seed 1 $TrainSetName naivebayes > naivebayes.ml
waffles_learn.exe test -seed 1 -confusion naivebayes.ml $TestSetName

echo -e "\nDecision tree:\n"
waffles_learn.exe train -seed 1 $TrainSetName decisiontree > decisiontree.ml
waffles_learn.exe test -seed 1 -confusion decisiontree.ml $TestSetName

echo -e "\nKNN 3:\n"
waffles_learn.exe train -seed 1 $TrainSetName knn -neighbors 3 > knn3.ml
waffles_learn.exe test -seed 1 -confusion knn3.ml $TestSetName

echo -e "\nCVDT 10:\n"
waffles_learn.exe train -seed 1 $TrainSetName cvdt 10 > cdvt10.ml
waffles_learn.exe test -seed 1 -confusion cdvt10.ml $TestSetName

echo -e "\n\n---------------\n"
echo -e "Sterilize the training set and test\n"
echo -e "---------------\n"

ShuffledDatasetName="train_shuffled_dataset_SummedContextSwitch.arff"
#Shuffle data first
waffles_transform.exe shuffle $TrainSetName -seed 1 > $ShuffledDatasetName

echo -e "\nAutotune Decision Tree:\n"
waffles_learn.exe autotune $ShuffledDatasetName decisiontree

echo -e "\nAutotune KNN:\n"
waffles_learn.exe autotune $ShuffledDatasetName knn

echo -e "\nSterilized Decision Tree:\n"
SterilizedDatasetName="train_sterilized_dataset_SummedContextSwitch.arff"
waffles_learn.exe sterilize -seed 1 -folds 10 $ShuffledDatasetName decisiontree > $SterilizedDatasetName
waffles_learn.exe train -seed 1 $SterilizedDatasetName decisiontree > decisiontree_sterlized.ml

echo -e "\n\n---------------\n"
echo -e "Precision/recall\n"
echo -e "---------------\n"
waffles_learn.exe precisionrecall -seed 1 -reps 10 $TrainSetName decisiontree