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

#### Spatial Relationship Operators (9 operators)
- **Contains** - Test if one geometry contains another
- **Intersects** - Test if geometries intersect  
- **Distance** - Calculate distance between geometries
- **Equals** - Test if two geometries are spatially equal
- **Disjoint** - Test if geometries don't intersect
- **Within** - Test if geometry1 is within geometry2
- **Crosses** - Test if geometries cross
- **Touches** - Test if geometries touch at boundaries
- **Overlaps** - Test if geometries of same dimension overlap

#### Geometry Operations
- **Buffer** - Create buffer (offset polygon) around geometry
- **ConvexHull** - Compute convex hull using Graham scan algorithm
- **Area** - Calculate area of polygons and envelopes
- **Length** - Calculate length/perimeter of geometries

#### Set Operations
- **Union** - Combine two geometries (all points in either geometry)
- **Intersection** - Find common areas (all points in both geometries)
- **Difference** - Subtract one geometry from another (geometry1 - geometry2)

#### Additional Operators
- **Simplify** - Simplify geometries using Douglas-Peucker algorithm
- **Centroid** - Calculate center of mass for geometries
- **Boundary** - Compute boundary per OGC specification

#### Advanced Operators
- **Clip** - Clip geometries to envelope using Cohen-Sutherland algorithm
- **GeodesicDistance** - Calculate great circle distance on WGS84 ellipsoid (Vincenty's formula)

### Import/Export Formats
- **WKT (Well-Known Text)** - Full import and export support for all geometry types
- **WKB (Well-Known Binary)** - Binary format import/export with endianness support
- **JSON** - Point serialization with System.Text.Json

### Spatial Reference System
- Support for well-known IDs (WKID)
- Built-in support for WGS 84 (EPSG:4326) and Web Mercator (EPSG:3857)

### JSON Serialization
- System.Text.Json converters for Point geometry
- Support for X, Y, Z, and M coordinates

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

### WKT Import/Export

```csharp
using Esri.Geometry.Core.IO;

// Export to WKT
var point = new Point(10.5, 20.7);
var wkt = WktExportOperator.ExportToWkt(point);
// Result: "POINT (10.5 20.7)"

// Import from WKT
var geometry = WktImportOperator.ImportFromWkt("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
var polygon = (Polygon)geometry;
```

### Spatial Relationship Tests

```csharp
// Test various spatial relationships
var env1 = new Envelope(0, 0, 10, 10);
var env2 = new Envelope(5, 5, 15, 15);

bool intersects = IntersectsOperator.Instance.Execute(env1, env2);  // true
bool overlaps = OverlapsOperator.Instance.Execute(env1, env2);      // true
bool disjoint = DisjointOperator.Instance.Execute(env1, env2);      // false
bool equals = EqualsOperator.Instance.Execute(env1, env1);          // true
```

### Geometry Operations

```csharp
// Create a buffer around a point
var point = new Point(10, 20);
var buffer = BufferOperator.Instance.Execute(point, 5.0);

// Compute convex hull
var multiPoint = new MultiPoint();
multiPoint.Add(new Point(0, 0));
multiPoint.Add(new Point(10, 0));
multiPoint.Add(new Point(5, 10));
var hull = ConvexHullOperator.Instance.Execute(multiPoint);

// Calculate area and length
var polygon = new Polygon();
// ... add rings to polygon
double area = AreaOperator.Instance.Execute(polygon);
double perimeter = LengthOperator.Instance.Execute(polygon);

// Simplify a polyline
var polyline = new Polyline();
// ... add paths
var simplified = SimplifyOperator.Instance.Execute(polyline, tolerance: 0.5);

// Calculate centroid
var centroid = CentroidOperator.Instance.Execute(polygon);

// Get boundary
var boundary = BoundaryOperator.Instance.Execute(polygon); // Returns Polyline

// Clip geometry to envelope
var clipEnvelope = new Envelope(0, 0, 100, 100);
var clipped = ClipOperator.Instance.Execute(polyline, clipEnvelope);

// Calculate geodesic distance (great circle on Earth)
var newYork = new Point(-74.0060, 40.7128);  // Lon, Lat
var london = new Point(-0.1278, 51.5074);
double distanceMeters = GeodesicDistanceOperator.Instance.Execute(newYork, london);
// Result: ~5,570,000 meters (5,570 km)
```

### Set Operations

```csharp
using Esri.Geometry.Core.Operators;

// Union - combine geometries
var point1 = new Point(0, 0);
var point2 = new Point(10, 10);
var union = UnionOperator.Instance.Execute(point1, point2); // Returns MultiPoint

var env1 = new Envelope(0, 0, 10, 10);
var env2 = new Envelope(5, 5, 15, 15);
var envUnion = UnionOperator.Instance.Execute(env1, env2); // Returns Envelope(0, 0, 15, 15)

// Intersection - find common areas
var intersection = IntersectionOperator.Instance.Execute(env1, env2); // Returns Envelope(5, 5, 10, 10)

var testPoint = new Point(7, 7);
var ptIntersection = IntersectionOperator.Instance.Execute(testPoint, env1); // Returns Point(7, 7)

// Difference - subtract one geometry from another
var mp = new MultiPoint();
mp.Add(new Point(2, 2));
mp.Add(new Point(5, 5));
mp.Add(new Point(12, 12));

var difference = DifferenceOperator.Instance.Execute(mp, env1); // Returns Point(12, 12) - points outside env1
```

### WKB Import/Export

```csharp
using Esri.Geometry.Core.IO;

// Export to WKB (binary format)
var point = new Point(10.5, 20.7);
byte[] wkb = WkbExportOperator.ExportToWkb(point);

// Export with big-endian byte order
byte[] wkbBigEndian = WkbExportOperator.ExportToWkb(point, bigEndian: true);

// Import from WKB
var geometry = WkbImportOperator.ImportFromWkb(wkb);
var parsedPoint = (Point)geometry;
```

## Project Structure

```
Esri.Geometry.Api/
├── src/
│   ├── Esri.Geometry.Core/          # Core geometry library
│   │   ├── Geometries/               # Geometry types
│   │   ├── Operators/                # Geometry operators
│   │   ├── SpatialReference/         # Spatial reference system
│   │   └── IO/                       # WKT and WKB import/export
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

### Completed Features ✅
- [x] All 9 spatial relationship operators (Contains, Intersects, Distance, Equals, Disjoint, Within, Crosses, Touches, Overlaps)
- [x] WKT (Well-Known Text) import/export for all geometry types
- [x] WKB (Well-Known Binary) import/export with endianness support
- [x] Buffer operator (simplified square/rectangular buffers)
- [x] Convex hull computation (Graham scan algorithm)
- [x] Area and Length calculation operators
- [x] Simplify operator (Douglas-Peucker algorithm)
- [x] Centroid calculation (center of mass)
- [x] Boundary computation (OGC specification)
- [x] Clip operator (Cohen-Sutherland line clipping)
- [x] Geodesic distance (Vincenty's formula on WGS84 ellipsoid)

### Test Coverage
- **152 tests passing** with comprehensive coverage
- 28 geometry type tests
- 14 spatial relationship operator tests
- 13 additional operator tests (Simplify, Centroid, Boundary)
- 12 geometry operation tests (Buffer, ConvexHull, Area, Length)
- 11 advanced operator tests (Clip, GeodesicDistance)
- 18 set operation tests (Union, Intersection, Difference)
- 17 WKT import/export tests
- 10 WKB import/export tests
- 4 JSON serialization tests

### Planned Features
- [ ] SymmetricDifference operator
- [ ] Enhanced GeoJSON import/export (beyond Point)
- [ ] Projection/transformation support
- [ ] Geodesic area calculations
- [ ] Performance optimizations using Span<T> and Memory<T>
- [ ] Circular buffer generation (currently only square/rectangular)
- [ ] Densify operator
- [ ] Offset operator

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
