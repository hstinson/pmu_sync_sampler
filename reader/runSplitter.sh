#Splits raw binary sample files into thread-based sample files,
#then filters the files and places them into a 'traces' folder
#so machine learning files can be created

#Verify input params
test $# -lt 3 && echo "runSplitter.sh <parent directory containing samples to be split> <Filter Type: MalwareTrain = 0, MalwareTest = 1, HamTrain = 2, HamTest = 3> <output directory>" >& 2 && exit 1
parentDir=$1
filterType=$2
outputDirectory=$3

echo "Parent Directory is $parentDir"

if [[ $parentDir != "" ]]; then
	if [[ -d $parentDir ]]; then
		echo "----------Splitting all files in $parentDir"
		FILES="$(find $parentDir -type f -iname '*.BIN')"
		for f in $FILES
		do
			# Get filename without extension
			filename=$(basename "$f")
			filename="${filename%.*}"
			filename=$parentDir/$filename
			
			echo "Creating directory $filename"
			
			./splitter $f $filename b
		done
		
		if [ "$filterType" == "2" ]
		then
			./SampleFileFilter $parentDir $filterType
		elif [ "$filterType" == "3" ] 
		then
			./SampleFileFilter $parentDir $filterType
		else
			./SampleFileFilter $parentDir $filterType blacklist.txt
		fi
	else
		echo "Error: $parentDir is not valid directory"
	fi	
fi




