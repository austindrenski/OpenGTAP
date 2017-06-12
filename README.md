# OpenGTAP
OpenGTAP is an exploratory project seeking to implement the GTAP (Global Trade Analysis Project) Model on the .NET Standard Framework.

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
