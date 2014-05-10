test $# -lt 3 && echo "runClassifiers.sh <directory of datasets> <training dataset> <testing dataset> " >& 2 && exit
DatasetDir=$1

#"test_dataset_SummedContextSwitch.arff"
#"train_dataset_SummedContextSwitch.arff"

TrainSetName=$2
TestSetName=$3

echo -e "\n\n---------------\n"
echo -e "Histogram Classification\n"
echo -e "---------------\n"

echo -e "\nNaive bayes:\n"
OutputNB="$DatasetDir/naiveBayes_histogramCtx.ml"
waffles_learn.exe train -seed 1 $TrainSetName naivebayes > "$OutputNB"
waffles_learn.exe test -seed 1 -confusion "$OutputNB" $TestSetName

echo -e "\nDecision tree:\n"
OutputDT="$DatasetDir/decisionTree_histogramCtx.ml"
waffles_learn.exe train -seed 1 $TrainSetName decisiontree > "$OutputDT"
waffles_learn.exe test -seed 1 -confusion "$OutputDT" $TestSetName

echo -e "\nKNN 3:\n"
OutputKNN="$DatasetDir/knn3_histogramCtx.ml"
waffles_learn.exe train -seed 1 $TrainSetName knn -neighbors 3 > "$OutputKNN"
waffles_learn.exe test -seed 1 -confusion "$OutputKNN" $TestSetName
