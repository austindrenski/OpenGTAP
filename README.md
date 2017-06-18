# OpenGTAP
OpenGTAP provides open-source tooling for the GTAP (Global Trade Analysis Project) Model on the .NET Framework.

![AppVeyor Build Status](https://ci.appveyor.com/api/projects/status/github/austindrenski/OpenGTAP?svg=true)[![Travis Build Status](https://travis-ci.org/austindrenski/OpenGTAP.svg?branch=master)](https://travis-ci.org/austindrenski/OpenGTAP)

## Current status
- The HeaderArrayConverter project is working on a data interchange format for the binary HAR and SL4 files.
    - Binary HAR and SL4 files can be read into memory.
    - In-memory representation can be serialized into a series of JSON files that are zipped into a HARX file.
    - HARX (zipped JSON) files can be read into memory.
    - In-memory representations of HAR, SL4, and HARX files can be written back to binary HAR files.
    
- Planning is underway for a lightweight cross-platform viewer application (ViewHARX?) capable of reading and writing HAR, SL4, and HARX files.

## The idea
The GTAP model provides a popular framework for conducting global economic analysis. Currently, the model is written in the GEMPACK (General Equilibrium Modeling PACKage) modeling software. Though powerful, GEMPACK is not freely available, nor does it provide open-source licensing. This project is interested in exploring the potential for an implementation of the standard GTAP model in a modern programming language running on an open-source framework, such as C# and the .NET Standard Framework.

The goal of OpenGTAP is not to replace the GEMPACK implementation of the GTAP model, but rather to provide a modern, object-oriented implementation that may encourage new developments from the open-source community.

## Potential areas for modernization
Working with C# 7 and targeting the .NET Standard Framework version 2.0, OpenGTAP seeks to simplify and build upon the standard model. 

The OpenGTAP team suspects that the following areas may see a performance benefit from modern abstractions: 
* HAR (Header Array) files could be replaced with a modern and accessible data format.
* Class systems could simplify the aggregation/disaggregation workflow that currently exists, making model customization simpler at the source code level.
* Data parallelism via the PLINQ (Parallel LINQ) library may simplify the data pipeline between model instantiation and computation. 
* The introduction of PLINQ-style abstractions could improve the readability of computationally intensive code—even if that code is itself not a candidate for parallelization.
