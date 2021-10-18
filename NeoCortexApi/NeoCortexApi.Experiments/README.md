# Introduction
This repository is the open source implementation of the Hierarchical Temporal Memory in C#/.NET Core. This repository contains set of libraries around **NeoCortext** API .NET Core library. **NeoCortex** API focuses implementation of _Hierarchical Temporal Memory Cortical Learning Algorithm_. Current version is first implementation of this algorithm on .NET platform. It includes the **Spatial Pooler**, **Temporal Pooler**, various encoders and **CorticalNetwork**  algorithms. Implementation of this library aligns to existing Python and JAVA implementation of HTM. Due similarities between JAVA and C#, current API of SpatialPooler in C# is very similar to JAVA API. However the implementation of future versions will include some API changes to API style, which is additionally more aligned to C# community.
This experiment is about Investigating Spatial Similarity in Spatial Pooler. This paper shows how to experiment with Spatial similarity. It first starts the Spatial Pooler and waits until it enters the stable state. The SP is trained with a few artificial vectors. The primary goal of this paper is to change the input parameter like InhibitionRadius, PotentialRadius, Global and LocalInhibition and observe how they influence the result. Then compare the current result with the past result. As SP tolerates small changes, we have to see that output SDR similarities for all vectors increased. 

## Getting started
The source code can be obtained in the below link :
https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityExperiment.cs

Follow the below steps to run the experiment 

1.Open NeoCortexAPI solution

2.Browse to NeoCortexApi.Experiments folder

3.Open File SpatialSimililarityExperiment.cs

4.Assign input images with parameters like image size, local area density value, potential radius value.
5.Run Test Case SpatialSimilarityExperimentImageTest
6.Image representations of non-zero input vector  and the resulting SDR is generated. Once you run the test case you can find this image vector in the following location
   \neocortexapi\NeoCortexApi\NeoCortexApi.Experiments\bin\Debug\net5.0\TestResults\SpatialSimilarityExperiment
7.For reference generated input vectors can be found here
    https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values/LocalAreaDensity%20%3D%200.pptx
8.A comparison table of all images exhibiting spatial similarities can be found in the text explorer.
9. Find the current comparison table (with different configuration paramters) and the graphical representation of the spatial similarity result in the below link : 
    https://github.com/JayashreeRegoti/neocortexapi/blob/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values/potentialIndipendent.xlsx
10. Comparision table for individual parameters can be found via below link:
    https://github.com/JayashreeRegoti/neocortexapi/tree/JayashreeRegoti/NeoCortexApi/NeoCortexApi.Experiments/SpatialSimilarityIndividualProjectJayashreeRegoti/Input%20And%20Output%20Values








