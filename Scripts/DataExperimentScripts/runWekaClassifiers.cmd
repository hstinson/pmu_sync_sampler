::Runs weka classifiers

@echo off

set TrainFile=%1
set TestFile=%2
set TestFileWithProcInfo=%3
set OutputDir=%4
set SkipOutlierRemoval=%5

if "%TrainFile%"=="" GOTO USAGE
if "%TestFile%"=="" GOTO USAGE
if "%TestFileWithProcInfo%"=="" GOTO USAGE
if "%OutputDir%"=="" GOTO USAGE

if not exist "%TrainFile%" GOTO USAGE
if not exist "%TestFile%" GOTO USAGE
if not exist "%TestFileWithProcInfo%" GOTO USAGE

if not exist "%OutputDir%" mkdir %OutputDir%

::Determine we are performing context-switch summing or histogram aggregation
::based on the file name
set FileInfo=histogram
set subStr=%TrainFile:Summed=%
if not %subStr% == %TrainFile% set FileInfo=contextSwitch

GOTO PERFORM

:USAGE
echo.
echo Invalid parameters or files do not exist.
echo USAGE: "%0 <training file> <testing file> <testing file with process info> <output dir>"
echo.
GOTO END

:PERFORM

set HeapSize=-Xmx12g
set JavaCommand=java %HeapSize%

::---------------------------------------------------------------------
:: Removing outliers and extreme values--------------------------------
::---------------------------------------------------------------------

:: We need to remove the the values specified by the next the last index.  Weka
:: has no next to last syntax so the values are hard-coded based on the aggregation
:: type.
set AttrIndex=8
if %FileInfo%==histogram set AttrIndex=68

set TrainFileWithOutliersRemoved=%OutputDir%train_%FileInfo%_outliersRemoved.arff
set TempFile=%OutputDir%train_temporary.arff
set TempFile2=%OutputDir%train_temporary2.arff
set ResampledTrainFile=%OutputDir%train_%FileInfo%_resampled.arff

if NOT "%SkipOutlierRemoval%"=="" (
	echo Skipping outlier removal...
	set TrainFileWithOutliersRemoved=%TrainFile%
	goto:Resample
)

:: If file already exists skip these steps
if exist %ResampledTrainFile% goto:Classify

set IQR_Command=weka.filters.unsupervised.attribute.InterquartileRange -R first-last -O 3.0 -E 6.0 -E-as-O
set RemoveWithVal_Command=weka.filters.unsupervised.instance.RemoveWithValues -S 0.0 -C %AttrIndex% -L last
set RemoveAttributes_Command=weka.filters.unsupervised.attribute.Remove -R %AttrIndex%,last

::Weka filters use -i for input -o for output
%JavaCommand% %IQR_Command% -i %TrainFile% -o %TempFile%
%JavaCommand% %RemoveWithVal_Command% -i %TempFile% -o %TempFile2%
%JavaCommand% %RemoveAttributes_Command% -i %TempFile2% -o %TrainFileWithOutliersRemoved%

del %TempFile%
del %TempFile2%


:Resample

:: Resample to balance the dataset------------------------------------
set Resample_Command=weka.filters.supervised.instance.Resample -B 1.0 -S 1 -Z 100.0 -c last
%JavaCommand% %Resample_Command% -i %TrainFileWithOutliersRemoved% -o %ResampledTrainFile%


:Classify

set J48_Classify_Command="weka.classifiers.trees.J48 -R -N 4 -Q 1 -M 2"
call:ClassificationFunction %J48_Classify_Command%,j48

set RandomForest_Classify_Command="weka.classifiers.trees.RandomForest -I 12 -K 0 -S 1"
call:ClassificationFunction %RandomForest_Classify_Command%,randomForest

goto:END

:ClassificationFunction
:: Args - Classification command, classification name to append to output files
:: This will performs the actual classification steps
:: -Trains the model
:: -Removes misclassfied feature vectors, then re-trains model
:: -Runs model on test set and saves results
:: -Merges prediction results with test set that contains the process info

set Classify_Command=%~1
set ModelType=%~2

echo Classify command: %Classify_Command%
echo Model Type: %ModelType%

:: Files-------------------
set ModelFile=%OutputDir%%ModelType%_%FileInfo%.model
set TestResultsFile=%OutputDir%testResults_%ModelType%_trainSet_%FileInfo%.txt
set RemoveMisclassTrainFile=%OutputDir%train_%ModelType%_%FileInfo%_removeMisclass.arff
set ModelFile_RemoveMissclass=%OutputDir%%ModelType%_%FileInfo%_removeMisclass.model
set TestResultsFile2=%OutputDir%testResults_%ModelType%_%FileInfo%_misclassRemoved.txt
set TempFile=%OutputDir%%ModelType%_temporaryData.arff
set ClassifiedResultsFile=%OutputDir%test_%ModelType%_%FileInfo%_predictionsOnly.arff
set CombinedTestSetAndResultsFile=%OutputDir%test_procInfo_%ModelType%_%FileInfo%_predictions.arff

if exist %ModelFile% goto:RemoveMisclassified
echo Perform classification on the train set to see what is not classified correctly
%JavaCommand% %Classify_Command% -t %ResampledTrainFile% -T %ResampledTrainFile% -d %ModelFile% -i > %TestResultsFile%

:RemoveMisclassified
if exist %RemoveMisclassTrainFile% goto:ReTrain
echo Remove misclassified from training set then re-sample
set RemoveMisclassified_Command=weka.filters.unsupervised.instance.RemoveMisclassified -W "weka.classifiers.misc.SerializedClassifier -model %ModelFile%" -C -1 -F 0 -T 0.1 -I 3 -c last 

%JavaCommand% %RemoveMisclassified_Command% -i %ResampledTrainFile% -o %RemoveMisclassTrainFile%

:ReTrain
if exist %ModelFile_RemoveMissclass% goto:Predict
echo Re-train the new classifier and perform classifcation on the test set
%JavaCommand% %Classify_Command% -t %RemoveMisclassTrainFile% -T %TestFile% -d %ModelFile_RemoveMissclass% -i > %TestResultsFile2%

:Predict
if exist %ClassifiedResultsFile% goto:Merge
echo Run prediction and save the results
set AddClassification_Command=weka.filters.supervised.attribute.AddClassification -classification -distribution -serialized %ModelFile_RemoveMissclass%
%JavaCommand% %AddClassification_Command% -i %TestFile% -o %TempFile% -c last

echo We drop the all the attributes not generated by the add classification command
set AttrIndexes=first-7
if %FileInfo%==histogram set AttrIndexes=first-67

set RemoveAttributes_Command=weka.filters.unsupervised.attribute.Remove -R %AttrIndexes%
%JavaCommand% %RemoveAttributes_Command% -i %TempFile% -o %ClassifiedResultsFile% -c last

del %TempFile%

:Merge
::@echo on
if exist %CombinedTestSetAndResultsFile% goto:GenMetrics
echo Merge the results file with the process info test set
set Merge_Command=weka.core.Instances merge %TestFileWithProcInfo% %ClassifiedResultsFile%

%JavaCommand% %Merge_Command% >> %CombinedTestSetAndResultsFile%

:GenMetrics
:: Generate the metrics-based ARFF and CSV files
set MetricsMalwareFile=%OutputDir%metrics_%ModelType%_%FileInfo%_malware.arff
set MetricsMalwareCsvFile=%OutputDir%metrics_%ModelType%_%FileInfo%_malware.csv

if exist %MetricsMalwareFile% del %MetricsMalwareFile%
if exist %MetricsMalwareCsvFile% del %MetricsMalwareCsvFile%

set RunTestSet_Command=weka.classifiers.misc.SerializedClassifier -model %ModelFile_RemoveMissclass% -t %RemoveMisclassTrainFile% -T %TestFile% -threshold-file %MetricsMalwareFile%

%JavaCommand% %RunTestSet_Command%
%JavaCommand% weka.core.converters.CSVSaver -i %MetricsMalwareFile% -o %MetricsMalwareCsvFile%
goto:eof

:END