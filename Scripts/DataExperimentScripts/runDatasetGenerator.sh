#Creates dataset files using various methods 

#Verify input params
test $# -lt 4 && echo "runDatasetGenerator.sh <train set dir> <test set dir> <output dir> <experiment number>" >& 2 && exit 1

TrainSetDir=$1
TestSetDir=$2
OutputDirectory=$3
ExperimentNumber=$4

IncludeProcInfo="-includeProcInfo"

#Samples between context switches
./SampleParser.exe "$TrainSetDir" "$OutputDirectory" "$ExperimentNumber" 2
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 2
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 2 "$IncludeProcInfo"

#Histogram ignore ctx switch
./SampleParser.exe "$TrainSetDir" "$OutputDirectory" "$ExperimentNumber" 6
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 6
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 6 "$IncludeProcInfo" 

#Histogram ctx switch
./SampleParser.exe "$TrainSetDir" "$OutputDirectory" "$ExperimentNumber" 7
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 7
./SampleParser.exe "$TestSetDir" "$OutputDirectory" "$ExperimentNumber" 7 "$IncludeProcInfo"