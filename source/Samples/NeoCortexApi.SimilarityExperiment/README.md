# Similarity Experiment

.....

# Abstract
.....

# Implementation




## Generate Training & Test Input SDR images

-  The below method is used to create input sdrs 

 <div class= "grey">

    public async Task CreateInputSdrs(string inputSdrDirectoryPath)

</div>


- In the file location if the Inputsdrs folder exists then it deletes the folder and creates the new 'inputsdrs' folder

<div class= "grey">

    if(Directory.Exists(inputSdrDirectoryPath))
            {
                Directory.Delete(inputSdrDirectoryPath, true);
            }
            
            Directory.CreateDirectory(inputSdrDirectoryPath);

</div> 
    


- It is calling each input sdr which is defined in InputSdrData.cs program.

 <div class= "grey">

    var inputSdrs = InputSdrData.GetInputSdrs();

</div>
     

-  Input SDR is defined in 2D array, instead of giving images we used this method so we have the exact dimension of image and find similarity have more accuracy.

![Input Sdr Data](InputSdrData.png)


- Later, we can find the input SDRs has been stored in InputSdrs folder. Input SDR images are stored in the below fashion.

![Input S D R Images](InputSDRImages.png)



 <div class= "grey">

    var inputSdrs = InputSdrData.GetInputSdrs();

</div>






## Start Similarity Experiment

## Configuration

## Fetching Training & TestInput SDR images

## Input Encoder

## Training & Test input SDRs

## Spatial Pooler

## Training & Test Output SDRs

## Foreach Output SDR

## Find Similarity via Classifier


![File](file.png)





