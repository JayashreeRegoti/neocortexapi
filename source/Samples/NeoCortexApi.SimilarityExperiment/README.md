# Similarity Experiment

.....

# Abstract
.....

# Implementation




## Generate Training & Test Input SDR images

-  The below method is used to create input sdrs 

 <div class= "grey">

    *public async Task CreateInputSdrs(string inputSdrDirectoryPath)*

</div>



![File1](file1.png)

- In the file location if the Inputsdrs folder exists then it deletes the folder and creates the new 'inputsdrs' folder

    *if(Directory.Exists(inputSdrDirectoryPath))
            {*
                *Directory.Delete(inputSdrDirectoryPath, true);
            }*
            *Directory.CreateDirectory(inputSdrDirectoryPath);*


- It is calling each input sdr which is defined in InputSdrData.cs program.

    *var inputSdrs = InputSdrData.GetInputSdrs();*






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





