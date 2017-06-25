# OpenGTAP: HeaderArrayConverter

HeaderArrayConverter is a C# library for manipulating header array (HAR) and solution (SL4) files.

## Installation

To install the HeaderArrayConverter library, run the following command in the Package Manager Console:

`PM> Install-Package HeaderArrayConverter`

## Getting Started

### Reading a binary header array file (.har) 

The following methods read the entire file and return a `HeaderArrayFile`:

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

The following methods read the entire file and return a `HeaderArrayFile`:

#### Synchronously:

```c#
HeaderArrayFile source = SolutionFile.BinaryReader.Read("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```

#### Asynchronously:

```c#
HeaderArrayFile source = await SolutionFile.BinaryReader.ReadAsync("filePath");

foreach (IHeaderArray array in source)
{
    Console.WriteLine(array);
}
```

### Reading a zipped JSON header array file (.harx) 

The following methods read the entire file and return a `HeaderArrayFile`:

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

### Enumerating header arrays from a binary header array file (.har)

The following methods yield one array at a time:

#### Synchronously:

```c#
foreach (IHeaderArray array in HeaderArrayFile.JsonReader.ReadArrays("filePath"))
{
    Console.WriteLine(array);
}
```

#### Asynchronously:

```c#
foreach (Task<IHeaderArray> array in HeaderArrayFile.JsonReader.ReadArraysAsync("filePath"))
{
    Console.WriteLine(await array);
}
```

### Enumerating header arrays from a zipped JSON header array file (.harx)

The following methods yield one array at a time:

#### Synchronously:

```c#
foreach (IHeaderArray array in HeaderArrayFile.JsonReader.ReadArrays("filePath"))
{
    Console.WriteLine(array);
}
```

#### Asynchronously:

```c#
foreach (Task<IHeaderArray> array in HeaderArrayFile.JsonReader.ReadArraysAsync("filePath"))
{
    Console.WriteLine(await array);
}
```
