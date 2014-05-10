test $# -lt 4 && echo "runExperimentClassifiers.sh <training dataset> <testing dataset> <keep columns string> <output directory>" >& 2 && exit

#"test_dataset_SummedContextSwitch.arff"
#"train_dataset_SummedContextSwitch.arff"

TrainSetName=$1
TestSetName=$2
KeepCols=$3
OutputDir=$4

mkdir $OutputDir

echo -s "Experiment With Keep Columns String of $KeepCols"

echo -e "\n\n---------------\n"
echo -e "Drop columns\n"
echo -e "---------------\n"

TrainSetReduced="$OutputDir/train_dataset_reduced_Summed.arff"
TestSetReduced="$OutputDir/test_dataset_reduced_Summed.arff"

waffles_transform.exe keeponlycolumns $TrainSetName $KeepCols > $TrainSetReduced
waffles_transform.exe keeponlycolumns $TestSetName $KeepCols > $TestSetReduced

echo -e "\n\n---------------\n"
echo -e "Context-switch based training and testing\n"
echo -e "---------------\n"

echo -e "\nNaive bayes:\n"
waffles_learn.exe train -seed 1 $TrainSetReduced naivebayes > "$OutputDir/naivebayes_reduced.ml"
waffles_learn.exe test -seed 1 -confusion "$OutputDir/naivebayes_reduced.ml" $TestSetReduced

echo -e "\nDecision tree:\n"
waffles_learn.exe train -seed 1 $TrainSetReduced decisiontree > "$OutputDir/decisiontree_reduced.ml"
waffles_learn.exe test -seed 1 -confusion "$OutputDir/decisiontree_reduced.ml" $TestSetReduced

echo -e "\nKNN 3:\n"
waffles_learn.exe train -seed 1 $TrainSetReduced knn -neighbors 3 > "$OutputDir/knn3_reduced.ml"
waffles_learn.exe test -seed 1 -confusion "$OutputDir/knn3_reduced.ml" $TestSetReduced

#echo -e "\nCVDT 10:\n"
#waffles_learn.exe train -seed 1 $TrainSetReduced cvdt 10 > "$OutputDir/cvdt10_reduced.ml"
#waffles_learn.exe test -seed 1 -confusion "$OutputDir/cvdt10_reduced.ml" $TestSetReduced

