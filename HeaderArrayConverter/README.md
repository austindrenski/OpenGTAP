# OpenGTAP: HeaderArrayConverter

HeaderArrayConverter is a C# library for manipulating header array (HAR) and solution (SL4) files.

## Installation

To install the HeaderArrayConverter library, run the following command in the Package Manager Console:

`PM> Install-Package HeaderArrayConverter`

## Getting Started

### Reading a binary header array file (.har) 

The `HeaderArrayFile` class provides a `Read(string)` method to read from a file path.

```c#
HeaderArrayFile source = HeaderArrayFile.Read("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```
