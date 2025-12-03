# Contributing to Esri Geometry API for .NET

Thank you for your interest in contributing to the Esri Geometry API for .NET! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code:
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on what is best for the community
- Show empathy towards others

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Your environment (OS, .NET version, etc.)
- Any relevant code samples or test cases

### Suggesting Enhancements

Enhancement suggestions are welcome! Please create an issue with:
- A clear, descriptive title
- Detailed description of the proposed enhancement
- Use cases demonstrating the value
- Any relevant examples from other libraries

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following our coding standards
3. **Add tests** for any new functionality
4. **Ensure all tests pass** with `dotnet test`
5. **Update documentation** as needed
6. **Submit a pull request**

## Development Setup

### Prerequisites

- .NET SDK 8.0 or later (for development)
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

### Getting Started

```bash
# Clone the repository
git clone https://github.com/znlgis/geometry-api-net.git
cd geometry-api-net

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run samples
cd samples/Esri.Geometry.Samples
dotnet run
```

## Coding Standards

### C# Style Guidelines

- Follow standard C# naming conventions:
  - PascalCase for types, methods, and properties
  - camelCase for parameters and local variables
  - _camelCase for private fields
- Use meaningful, descriptive names
- Keep methods focused and concise
- Add XML documentation comments for all public APIs
- Use nullable reference types appropriately

### Example

```csharp
namespace Esri.Geometry.Core.Geometries
{
    /// <summary>
    /// Represents a point with X and Y coordinates.
    /// </summary>
    public class Point : Geometry
    {
        /// <summary>
        /// Gets or sets the X coordinate.
        /// </summary>
        public double X { get; set; }
        
        /// <summary>
        /// Gets or sets the Y coordinate.
        /// </summary>
        public double Y { get; set; }
        
        /// <summary>
        /// Calculates the distance to another point.
        /// </summary>
        /// <param name="other">The other point.</param>
        /// <returns>The distance between the points.</returns>
        public double Distance(Point other)
        {
            // Implementation
        }
    }
}
```

## Testing Guidelines

### Writing Tests

- Use xUnit for all tests
- Follow the Arrange-Act-Assert pattern
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Test both success and failure cases
- Test edge cases and boundary conditions

### Example Test

```csharp
[Fact]
public void Point_Distance_CalculatesCorrectDistance()
{
    // Arrange
    var point1 = new Point(0, 0);
    var point2 = new Point(3, 4);
    
    // Act
    var distance = point1.Distance(point2);
    
    // Assert
    Assert.Equal(5, distance, 10);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for a specific project
dotnet test tests/Esri.Geometry.Tests/Esri.Geometry.Tests.csproj
```

## Project Structure

```
geometry-api-net/
├── src/
│   ├── Esri.Geometry.Core/      # Core geometry library
│   │   ├── Geometries/           # Geometry types
│   │   ├── Operators/            # Operators
│   │   ├── SpatialReference/     # Spatial reference
│   │   └── IO/                   # I/O support
│   └── Esri.Geometry.Json/       # JSON support
├── tests/
│   └── Esri.Geometry.Tests/      # Unit tests
├── samples/
│   └── Esri.Geometry.Samples/    # Sample applications
└── docs/                         # Documentation
```

## Documentation

- Update the README.md if you add new features
- Add XML documentation comments to all public APIs
- Update inline code comments for complex logic
- Add examples to the samples project

## Commit Messages

Follow these guidelines for commit messages:

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters
- Reference issues and pull requests when relevant

### Examples

```
Add Buffer operator implementation

Implement the Buffer geometry operator with support for:
- Distance-based buffering
- Multiple buffer rings
- Flat and round endcaps

Fixes #123
```

## Release Process

1. Update version numbers in project files
2. Update CHANGELOG.md
3. Create a git tag
4. Build and publish NuGet packages
5. Create GitHub release with notes

## Questions?

If you have questions about contributing, feel free to:
- Open an issue for discussion
- Contact the maintainers
- Check existing issues and pull requests

## License

By contributing, you agree that your contributions will be licensed under the LGPL 2.1 License.
