using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;
using Esri.Geometry.Core.SpatialReference;

Console.WriteLine("===== Esri Geometry API for .NET - Sample Usage =====\n");

// 1. Create and work with points
Console.WriteLine("1. Working with Points:");
var point1 = new Point(10, 20);
var point2 = new Point(30, 40);
Console.WriteLine($"   Point 1: ({point1.X}, {point1.Y})");
Console.WriteLine($"   Point 2: ({point2.X}, {point2.Y})");
Console.WriteLine($"   Distance: {point1.Distance(point2):F2}\n");

// 2. Create and work with envelopes
Console.WriteLine("2. Working with Envelopes:");
var envelope = new Envelope(0, 0, 100, 100);
Console.WriteLine($"   Envelope bounds: ({envelope.XMin}, {envelope.YMin}) to ({envelope.XMax}, {envelope.YMax})");
Console.WriteLine($"   Width: {envelope.Width}, Height: {envelope.Height}");
Console.WriteLine($"   Area: {envelope.Area}");
Console.WriteLine($"   Center: ({envelope.Center.X}, {envelope.Center.Y})\n");

// 3. Test containment
Console.WriteLine("3. Testing Containment:");
var testPoint = new Point(50, 50);
Console.WriteLine($"   Does envelope contain point ({testPoint.X}, {testPoint.Y})? {envelope.Contains(testPoint)}");
var outsidePoint = new Point(150, 150);
Console.WriteLine($"   Does envelope contain point ({outsidePoint.X}, {outsidePoint.Y})? {envelope.Contains(outsidePoint)}\n");

// 4. Create a multi-point geometry
Console.WriteLine("4. Working with MultiPoint:");
var multiPoint = new MultiPoint();
multiPoint.Add(new Point(10, 10));
multiPoint.Add(new Point(20, 20));
multiPoint.Add(new Point(30, 30));
Console.WriteLine($"   MultiPoint has {multiPoint.Count} points");
var mpEnvelope = multiPoint.GetEnvelope();
Console.WriteLine($"   Envelope: ({mpEnvelope.XMin}, {mpEnvelope.YMin}) to ({mpEnvelope.XMax}, {mpEnvelope.YMax})\n");

// 5. Create a polyline
Console.WriteLine("5. Working with Polyline:");
var polyline = new Polyline();
var path = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10) };
polyline.AddPath(path);
Console.WriteLine($"   Polyline has {polyline.PathCount} path(s)");
Console.WriteLine($"   Total length: {polyline.Length:F2}\n");

// 6. Create a polygon
Console.WriteLine("6. Working with Polygon:");
var polygon = new Polygon();
var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
polygon.AddRing(ring);
Console.WriteLine($"   Polygon has {polygon.RingCount} ring(s)");
Console.WriteLine($"   Area: {polygon.Area:F2}\n");

// 7. Use operators
Console.WriteLine("7. Using Operators:");
var distanceOp = DistanceOperator.Instance;
var distance = distanceOp.Execute(point1, point2);
Console.WriteLine($"   Distance between points using operator: {distance:F2}");

var containsOp = ContainsOperator.Instance;
var contains = containsOp.Execute(envelope, testPoint);
Console.WriteLine($"   Contains check using operator: {contains}");

var intersectsOp = IntersectsOperator.Instance;
var envelope2 = new Envelope(50, 50, 150, 150);
var intersects = intersectsOp.Execute(envelope, envelope2);
Console.WriteLine($"   Do envelopes intersect? {intersects}\n");

// 8. Spatial reference
Console.WriteLine("8. Working with Spatial Reference:");
var wgs84 = SpatialReference.Wgs84();
Console.WriteLine($"   WGS 84 WKID: {wgs84.Wkid}");
var webMercator = SpatialReference.WebMercator();
Console.WriteLine($"   Web Mercator WKID: {webMercator.Wkid}\n");

Console.WriteLine("===== Sample Complete =====");

