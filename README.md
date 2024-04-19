# JsonCollectionNet

`JsonCollectionNet` is a library designed to perform aggregation queries on JSON data. Inspired by MongoDB's aggregation pipeline, this library is designed to allow efficient handling and analysis of JSON data in C#. Users can filter, group, sort, and limit data using simple JSON-based queries.

## Features

- **Filtering**: Use `$match` to filter data based on conditions.
- **Grouping**: Use `$group` to group data by specified criteria and perform aggregations.
- **Sorting**: Use `$sort` to sort data.
- **Limiting**: Use `$limit` to restrict the number of results returned.

## Installation

`JsonCollectionNet` is available as a NuGet package. Install it using the following NuGet command:

```
dotnet add package JsonCollectionNet
```


## Usage

### Initializing JSON Data

```csharp
using JsonCollectionNet;
using System.Text.Json;

var json = @"[
    { ""id"": 1, ""name"": ""Alice"", ""age"": 30 },
    { ""id"": 2, ""name"": ""Bob"", ""age"": 25 }
]";
JsonElement data = JsonSerializer.Deserialize<JsonElement>(json);
```

### Aggregating Data

```csharp
var collection = new JsonCollection(data);

// Filter users older than 30
var aggregateQuery = @"{ $match: { age: { $gt: 30 } } }";
JsonElement result = collection.Aggregate(aggregateQuery);
```

## Support and Contributions
- **Bug reports and feature requests**: Please use the GitHub issue tracker.
- **Contributions**: You are welcome to contribute via pull requests. Please read CONTRIBUTING.md before contributing.

---
This library was created for developers who want to easily handle complex JSON data structures. Simplify your data processing with JsonCollectionNet!