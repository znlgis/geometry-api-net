# Esri Geometry API for .NET

A C# port of the [Esri Geometry API for Java](https://github.com/Esri/geometry-api-java) targeting .NET Standard 2.0, providing comprehensive geometry operations for spatial data analysis.

## Overview

This library provides a complete set of geometry types and spatial operations compatible with Esri's geometry model. It is designed for cross-platform use with .NET Core, .NET Framework 4.6.1+, Xamarin, and other .NET Standard 2.0 compatible platforms.

## Features

### Geometry Types
- **Point** - Represents a point with X, Y coordinates (optional Z and M values)
- **MultiPoint** - Collection of points
- **Polyline** - One or more connected paths
- **Polygon** - One or more rings forming polygons
- **Envelope** - Axis-aligned bounding rectangle
- **Line** - Line segment between two points

### Spatial Operators
- **Distance** - Calculate distance between geometries
- **Contains** - Test if one geometry contains another
- **Intersects** - Test if geometries intersect
- More operators to be implemented...

### Spatial Reference System
- Support for well-known IDs (WKID)
- Built-in support for WGS 84 (EPSG:4326) and Web Mercator (EPSG:3857)
- Well-Known Text (WKT) support

### JSON Serialization
- System.Text.Json converters for geometry types
- Support for Esri JSON format

## Getting Started

### Installation

Add reference to the core library in your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Esri.Geometry.Core/Esri.Geometry.Core.csproj" />
</ItemGroup>
```

For JSON support, also reference:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Esri.Geometry.Json/Esri.Geometry.Json.csproj" />
</ItemGroup>
```

### Basic Usage

```csharp
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

// Create points
var point1 = new Point(10, 20);
var point2 = new Point(30, 40);

// Calculate distance
var distance = point1.Distance(point2);
Console.WriteLine($"Distance: {distance}");

// Create an envelope
var envelope = new Envelope(0, 0, 100, 100);

// Test containment
var testPoint = new Point(50, 50);
bool contains = envelope.Contains(testPoint);
Console.WriteLine($"Contains: {contains}");

// Use operators
var distanceOp = DistanceOperator.Instance;
var dist = distanceOp.Execute(point1, point2);

var containsOp = ContainsOperator.Instance;
var result = containsOp.Execute(envelope, testPoint);
```

### Working with Polygons

```csharp
var polygon = new Polygon();
var ring = new[] {
    new Point(0, 0),
    new Point(10, 0),
    new Point(10, 10),
    new Point(0, 10),
    new Point(0, 0)
};
polygon.AddRing(ring);

var area = polygon.Area;
Console.WriteLine($"Polygon area: {area}");
```

### Spatial Reference

```csharp
using Esri.Geometry.Core.SpatialReference;

// Create WGS 84 spatial reference
var wgs84 = SpatialReference.Wgs84();
Console.WriteLine($"WKID: {wgs84.Wkid}");

// Create Web Mercator spatial reference
var webMercator = SpatialReference.WebMercator();
```

## Project Structure

```
Esri.Geometry.Api/
├── src/
│   ├── Esri.Geometry.Core/          # Core geometry library
│   │   ├── Geometries/               # Geometry types
│   │   ├── Operators/                # Geometry operators
│   │   ├── SpatialReference/         # Spatial reference system
│   │   └── IO/                       # I/O support (future)
│   └── Esri.Geometry.Json/           # JSON serialization support
├── tests/
│   └── Esri.Geometry.Tests/          # Unit tests (xUnit)
└── samples/
    └── Esri.Geometry.Samples/        # Sample applications
```

## Building the Project

### Prerequisites
- .NET SDK 8.0 or later (for building)
- The library targets .NET Standard 2.0 for maximum compatibility

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run the sample application
cd samples/Esri.Geometry.Samples
dotnet run
```

## Testing

The project uses xUnit for unit testing. Tests are located in the `tests/Esri.Geometry.Tests` directory.

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Technology Stack

- **Target Framework**: .NET Standard 2.0
- **Language**: C# 7.0+
- **JSON Library**: System.Text.Json 8.0.6
- **Testing Framework**: xUnit
- **License**: LGPL 2.1

## Roadmap

### Planned Features
- [ ] Additional spatial operators (Buffer, Union, Intersection, Difference, etc.)
- [ ] WKT (Well-Known Text) import/export
- [ ] WKB (Well-Known Binary) import/export
- [ ] Projection/transformation support
- [ ] Simplification and generalization algorithms
- [ ] Spatial relationship operators (Touches, Within, Overlaps, Crosses, Disjoint)
- [ ] Convex hull computation
- [ ] Geodesic calculations
- [ ] Performance optimizations using Span<T> and Memory<T>

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the GNU Lesser General Public License v2.1 - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

This project is a C# port of the [Esri Geometry API for Java](https://github.com/Esri/geometry-api-java), which is licensed under the Apache 2.0 license.

## Related Projects

- [Esri Geometry API for Java](https://github.com/Esri/geometry-api-java) - The original Java implementation
- [geometry-api-cs](https://github.com/Esri/geometry-api-cs) - Another .NET implementation by Esri

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/znlgis/geometry-api-net).
