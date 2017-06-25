# OpenGTAP: HeaderArrayConverter

HeaderArrayConverter is a C# library for manipulating header array (HAR) and solution (SL4) files.

## Installation

To install the HeaderArrayConverter library, run the following command in the Package Manager Console:

`PM> Install-Package HeaderArrayConverter`

## Getting Started

### Reading a binary header array file (.har) 

#### Synchronously:

```c#
HeaderArrayFile source = HeaderArrayFile.BinaryReader.Read("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```

#### Asynchronously:

```c#
HeaderArrayFile source = await HeaderArrayFile.BinaryReader.ReadAsync("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```

### Reading a binary solution file (.sl4)

#### Synchronously;

```c#
HeaderArrayFile source = SolutionFile.BinaryReader.Read("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```

#### Asynchronously;

```c#
HeaderArrayFile source = await SolutionFile.BinaryReader.ReadAsync("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```
