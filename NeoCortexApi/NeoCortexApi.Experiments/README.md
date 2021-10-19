# Spatial Similarity Experiment

## Introduction
This repository is the open source implementation of the Hierarchical Temporal Memory in C#/.NET Core. This repository contains set of libraries around **NeoCortext** API .NET Core library. **NeoCortex** API focuses implementation of _Hierarchical Temporal Memory Cortical Learning Algorithm_. Current version is first implementation of this algorithm on .NET platform. It includes the **Spatial Pooler**, **Temporal Pooler**, various encoders and **CorticalNetwork**  algorithms. Implementation of this library aligns to existing Python and JAVA implementation of HTM. Due similarities between JAVA and C#, current API of SpatialPooler in C# is very similar to JAVA API. However the implementation of future versions will include some API changes to API style, which is additionally more aligned to C# community.
This experiment is about Investigating Spatial Similarity in Spatial Pooler. This paper shows how to experiment with Spatial similarity. It first starts the Spatial Pooler(SP) and waits until it enters the stable state. The SP is trained with a few artificial vectors. The primary goal of this paper is to change the input parameter like InhibitionRadius, PotentialRadius, Global and LocalInhibition and observe how they influence the result. Then compare the current result with the past result. As SP tolerates small changes, we have to see that output SDR similarities for all vectors increased. 

## Aim of the experiment
We try to find out the Spatial similarity of the Spatial pooler by changing configuration parameters in the experiment and see how these changes can influence results. The spatial pooler, in this case, can make a difference of vectors more significant than it is. As it is suitable to recognize minor details. The output has recorded the change in percentage.

The source code can be obtained in the below link :
https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityExperiment.cs


## Getting started

1.Open NeoCortexAPI solution

2.Browse to NeoCortexApi.Experiments folder

3.Open File SpatialSimililarityExperiment.cs


First step in using of any algorithm is initialization of various parameters, which are defined by class `SpatialSimilarityExperiment`.
 
```csharp
public void SpatialSimilarityExperimentImageTest(string[] testImageFileNames, int imageSize, double localAreaDensityValue, double potentialRadiusValue)
{

    Console.WriteLine($"Hello {nameof(SpatialSimilarityExperiment)} experiment.");
    // Used as a boosting parameters
    // that ensure homeostatic plasticity effect.
    double minOctOverlapCycles = 1.0;
    double maxBoost = 5.0;

    // We will use (square of image size) bits to represent an input vector (pattern).
    int inputBits = imageSize * imageSize;

    // We will build a slice of the cortex with the given number of mini-columns
    int numColumns = 2048;

    // This is a set of configuration parameters used in the experiment.
    HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
    {
        CellsPerColumn = 10,
        MaxBoost = maxBoost,
        DutyCyclePeriod = 100,
        MinPctOverlapDutyCycles = minOctOverlapCycles,
        StimulusThreshold = 5,
        GlobalInhibition = false,
        NumActiveColumnsPerInhArea = 0.02 * numColumns,
        PotentialRadius = (int)(potentialRadiusValue * inputBits),
        LocalAreaDensity = localAreaDensityValue,// - 1,//0.5,
        ActivationThreshold = 10,
        MaxSynapsesPerSegment = (int)(0.01 * numColumns),
        Random = new ThreadSafeRandom(42)
    };
```
Kindly note:  The value of Image size, PotentialRadius and LocalAreaDensity need be assigned while assigning the the input images.

where,

|Parameter Name  |  Meaning|
|--|--|
|POTENTIAL_RADIUS  | Defines the radius in number of input cells visible to column cells. It is important to choose this value, so every input neuron is connected to at least a single column. For example, if the input has 50000 bits and the column topology is 500, then you must choose some value larger than 50000/500 > 100.  |
|GLOBAL_INHIBITION  | If TRUE global inhibition algorithm will be used. If FALSE local inhibition algorithm will be used. |
|LOCAL_AREA_DENSITY  | Density of active columns inside of local inhibition radius. If set on value < 0, explicit number of active columns (NUM_ACTIVE_COLUMNS_PER_INH_AREA) will be used.|
|NUM_ACTIVE_COLUMNS_PER_INH_AREA | An alternate way to control the density of the active columns. If this value is specified then LOCAL_AREA_DENSITY must be less than 0, and vice versa.
|STIMULUS_THRESHOLD| One mini-column is active if its overlap exceeds _overlap threshold_ **θo** of connected synapses.  |
|SYN_PERM_CONNECTED| Defines _Connected Permanence Threshold_ **θp**, which is a float value, which must be exceeded to declare synapse as connected.  |
|DUTY_CYCLE_PERIOD| Number of iterations. The period used to calculate duty cycles. Higher values make it take longer to respond to changes in boost. Shorter values make it more unstable and likely to oscillate.  |
|MAX_BOOST| Maximum boost factor of a column.  |

Chosing right parameters is very important as we need to find the range where our system can memorize to its maximum limits.
For example, following method will set  SpatialPooler to use local inhibition with very long period for boosting (DUTY_CYCLE_PERIOD) to ensure stable representation. It will use 10 columns as potential radius (POTENTIAL_RADIUS), which means that every column (cell, which represents column) will see 10 input cells. This is called column receptive field.


4.Assign input images with parameters like image size, local area density value, potential radius value.

The input need to assigned in this format

```csharp
[DataTestMethod]
[DataRow(new string[] { "box1_8.png", "box1_9.png", "box1_10.png", "box1_11.png", "box1_12.png" , "box1_13.png" , "box1_14.png" , "box1_15.png" , "box1_16.png", "box1_17.png" }, 15, 0.5, 0.45)],
[DataRow(new string[] { "box1_8.png", "box1_9.png", "box1_10.png", "box1_11.png", "box1_12.png" , "box1_13.png" , "box1_14.png" , "box1_15.png" , "box1_16.png", "box1_17.png" }, 15, 0.3, 0.5)],
[DataRow(new string[] { "box1_8.png", "box1_9.png", "box1_10.png", "box1_11.png", "box1_12.png" , "box1_13.png" , "box1_14.png" , "box1_15.png" , "box1_16.png", "box1_17.png" }, 15, 0.5, 0.20)],

```

and these values are called here

```csharp
public void SpatialSimilarityExperimentImageTest(string[] testImageFileNames, int imageSize, double localAreaDensityValue, double potentialRadiusValue)
```

5.Run Test Case SpatialSimilarityExperimentImageTest

The primary thing the test does is to convert the input image to binary data. Below is the source code used for the same. You can find ths under method 'GetInputVectorsFromImages'

```csharp
Binarizer binarizer = new Binarizer(200, 200, 200, imageSize, imageSize);
binarizer.CreateBinary(Path.Combine(TestDataFullPath, testImageFileName), binarizerFileName);
var lines = File.ReadAllLines(binarizerFileName);

var inputLine = new List<int>();
foreach (var line in lines)
{
    var lineValues = new List<int>();
    foreach (var character in line)
    {
        if (Int32.TryParse(character.ToString(), out int bitValue))
        {
            lineValues.Add(bitValue);
        }
        else
        {
            lineValues.Add(0);
        }
    }
    inputLine.AddRange(lineValues);
}
```

You can find 2D binary array which is created in in this location `\neocortexapi\NeoCortexApi\NeoCortexApi.Experiments\bin\Debug\net5.0\TestResults\SpatialSimilarityExperiment`. The 2D binary image is again converted into 1D training array.

6.Then to get training vectors it verifies the following condition

```csharp
else if (experimentCode == 2)
    {
        if (testImageFileNames == null || imageSize == 0)
        {
            return new List<int[]>();
        }

        return GetInputVectorsFromImages(testImageFileNames, imageSize);
    } 
```

Image representations of non-zero input vector  and the resulting SDR is generated. Once you run the test case you can find this image vector in the following location:
`\neocortexapi\NeoCortexApi\NeoCortexApi.Experiments\bin\Debug\net5.0\TestResults\SpatialSimilarityExperiment`.

For reference generated input vectors can be found here
    https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values/LocalAreaDensity%20%3D%200.pptx


7. Now the experiments goes into `RunExperiment` method where the implemenatation of experiment starts. The first thing it does is to check whether the system has reached stable stage or not and this is done by the below code

```csharp
HomeostaticPlasticityController hpa = new HomeostaticPlasticityController(mem, inputValues.Count * 40,
    (isStable, numPatterns, actColAvg, seenInputs) =>
    {
        // Event should only be fired when entering the stable state.
        // Ideal SP should never enter unstable state after stable state.
        if (isStable == false)
        {
            Debug.WriteLine($"INSTABLE STATE");
            // This should usually not happen.
            isInStableState = false;
        }
        else
        {
            Debug.WriteLine($"STABLE STATE");
            // Here you can perform any action if required.
            isInStableState = true;
        }
    }, requiredSimilarityThreshold: 0.975);
```


Kindly note:  minimum **required Similarity Threshold** should be 0.975 for this experiment.


As next **SpatialPooler** creates the instance of Spatial Pooler Multithreaded version. Following code shows how to do this:
  
```csharp
// It creates the instance of Spatial Pooler Multithreaded version.
SpatialPooler sp = new SpatialPoolerMT(hpa);

// Initializes the `SpatialPooler
sp.Init(mem);
```

Then it learns the compute of spatial pooler object then it gets overlap over every single column.
Here we boost calculated Overlaps. This is called Homeostatic Plasticity Mechanism


```csharp
var overlaps = CalculateOverlap(this.connections, inputVector);

this.connections.Overlaps = overlaps;

double[] boostedOverlaps;

```

Once the overlap is done the system tries to learns all the necessary factors which are mentioned below

```csharp
if (learn)
{
    AdaptSynapses(this.connections, inputVector, activeColumns);
    UpdateDutyCycles(this.connections, overlaps, activeColumns);
    BumpUpWeakColumns(this.connections);
    UpdateBoostFactors(this.connections);
    if (IsUpdateRound(this.connections))
    {
        UpdateInhibitionRadius(this.connections);
        UpdateMinDutyCycles(this.connections);
    }
}

```
and once the enough rounds are passed it goes into stable state. Then we generate the result, which draws all the inputs are related SDR’s. 
It also outputs the similarity matrix which is shown as below.


Debug Trace:
STABLE STATE

|Input Image || I-00            | I-01            | I-02            | I-03            | I-04            | I-05            | I-06            | I-07            | I-08            | I-09            |
|--|--|
 I-00            | 100/100         | 62.5/76.17      | 39.29/71.48     | 33.33/70.9      | 31.43/74.61     | 21.57/72.56     | 17.19/70.21     | 15.28/69.43     | 9.91/59.57      | 9.09/51.07      |
 I-01            | 62.5/76.17      | 100/100         | 57.14/91.31     | 55.56/91.31     | 45.71/90.82     | 31.37/88.09     | 25/88.28        | 22.22/87.79     | 14.41/76.46     | 13.22/68.65     |
 I-02            | 39.29/71.48     | 57.14/91.31     | 100/100         | 89.29/98.34     | 80/87.99        | 54.9/86.72      | 43.75/88.57     | 38.89/87.99     | 25.23/77.25     | 23.14/71.39     |
 I-03            | 33.33/70.9      | 55.56/91.31     | 89.29/98.34     | 100/100         | 77.14/88.09     | 52.94/87.21     | 42.19/89.16     | 37.5/88.57      | 24.32/77.64     | 22.31/71.88     |
 I-04            | 31.43/74.61     | 45.71/90.82     | 80/87.99        | 77.14/88.09     | 100/100         | 68.63/92.87     | 54.69/89.65     | 48.61/88.96     | 31.53/78.61     | 28.93/67.68     |
 I-05            | 21.57/72.56     | 31.37/88.09     | 54.9/86.72      | 52.94/87.21     | 68.63/92.87     | 100/100         | 79.69/93.65     | 70.83/92.97     | 45.95/82.23     | 42.15/70.51     |
 I-06            | 17.19/70.21     | 25/88.28        | 43.75/88.57     | 42.19/89.16     | 54.69/89.65     | 79.69/93.65     | 100/100         | 88.89/97.36     | 57.66/85.06     | 52.89/74.8      |
 I-07            | 15.28/69.43     | 22.22/87.79     | 38.89/87.99     | 37.5/88.57      | 48.61/88.96     | 70.83/92.97     | 88.89/97.36     | 100/100         | 64.86/86.23     | 59.5/75.98      |
 I-08            | 9.91/59.57      | 14.41/76.46     | 25.23/77.25     | 24.32/77.64     | 31.53/78.61     | 45.95/82.23     | 57.66/85.06     | 64.86/86.23     | 100/100         | 91.74/85.74     |
 I-09            | 9.09/51.07      | 13.22/68.65     | 23.14/71.39     | 22.31/71.88     | 28.93/67.68     | 42.15/70.51     | 52.89/74.8      | 59.5/75.98      | 91.74/85.74     | 100/100         |

A comparison table of all images exhibiting spatial similarities can be found in the text explorer. The above values can be compared with other comparision matrix with different configuration parameters and see, over which range is the system memorizing the most.


9.Find the current comparison table (with different configuration paramters) and the graphical representation of the spatial similarity result in the below link : 
    https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values/potentialIndipendent.xlsx

10.Comparision table for individual parameters can be found via below link:
    https://github.com/JayashreeRegoti/neocortexapi/tree/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values

```csharp
```



Here are some images for your reference, however this images can also be found in the documentation.

Image of 2D binary array created after binarizer is done
![image.png](/.assets/ImageBinarizing.png)

Image generated after method `GetTrainingvectors` is performed
![image.png](/.assets/GetinputVector.png)
