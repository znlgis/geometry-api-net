# Changelog

All notable changes to the Esri Geometry API for .NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial implementation of core geometry types
  - Point with X, Y, Z, and M coordinates
  - MultiPoint for collections of points
  - Line for line segments
  - Polyline for multi-path line geometries
  - Polygon for area geometries
  - Envelope for bounding rectangles
- Base Geometry abstract class with common properties and methods
- GeometryType enumeration for geometry type identification
- Spatial reference system support
  - SpatialReference class with WKID and WKT support
  - Built-in WGS 84 and Web Mercator spatial references
- Basic geometry operators
  - Distance operator for calculating distances between geometries
  - Contains operator for containment tests
  - Intersects operator for intersection tests
- JSON serialization support
  - PointJsonConverter for System.Text.Json
- Comprehensive test suite with 61+ unit tests using xUnit
- Sample application demonstrating library usage
- Complete project documentation
  - README.md with usage examples
  - CONTRIBUTING.md with contribution guidelines
  - XML documentation for all public APIs

### Technical Details
- Targets .NET Standard 2.0 for maximum compatibility
- Uses C# 7.0+ language features
- Zero third-party dependencies in core library
- System.Text.Json 8.0.6 for JSON support
- Cross-platform support (Windows, Linux, macOS)

## [0.1.0] - Initial Release

### Project Structure
- **Esri.Geometry.Core** - Core geometry library
- **Esri.Geometry.Json** - JSON serialization support
- **Esri.Geometry.Tests** - Unit test project
- **Esri.Geometry.Samples** - Sample applications

[Unreleased]: https://github.com/znlgis/geometry-api-net/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/znlgis/geometry-api-net/releases/tag/v0.1.0
