using Xunit;
using Esri.Geometry.Core;
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators
{
    public class GeometryEngineTests
    {
        [Fact]
        public void TestGeometryEngineContains()
        {
            var envelope = new Envelope(0, 0, 100, 100);
            var point = new Point(50, 50);

            bool result = GeometryEngine.Contains(envelope, point);

            Assert.True(result);
        }

        [Fact]
        public void TestGeometryEngineIntersects()
        {
            var env1 = new Envelope(0, 0, 10, 10);
            var env2 = new Envelope(5, 5, 15, 15);

            bool result = GeometryEngine.Intersects(env1, env2);

            Assert.True(result);
        }

        [Fact]
        public void TestGeometryEngineDistance()
        {
            var point1 = new Point(0, 0);
            var point2 = new Point(3, 4);

            double distance = GeometryEngine.Distance(point1, point2);

            Assert.Equal(5.0, distance);
        }

        [Fact]
        public void TestGeometryEngineBuffer()
        {
            var point = new Point(10, 20);
            var buffer = GeometryEngine.Buffer(point, 5.0);

            Assert.NotNull(buffer);
            // Buffer can return either Envelope or Polygon depending on implementation
            Assert.True(buffer is Envelope || buffer is Polygon);
        }

        [Fact]
        public void TestGeometryEngineArea()
        {
            var polygon = new Polygon();
            polygon.AddRing(new[] {
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 10),
                new Point(0, 10),
                new Point(0, 0)
            });

            double area = GeometryEngine.Area(polygon);

            Assert.Equal(100.0, area);
        }

        [Fact]
        public void TestGeometryEngineWktRoundTrip()
        {
            var point = new Point(10.5, 20.7);
            
            string wkt = GeometryEngine.GeometryToWkt(point);
            var geometry = GeometryEngine.GeometryFromWkt(wkt);

            Assert.IsType<Point>(geometry);
            var resultPoint = (Point)geometry;
            Assert.Equal(10.5, resultPoint.X, 5);
            Assert.Equal(20.7, resultPoint.Y, 5);
        }

        [Fact]
        public void TestGeometryEngineGeoJsonRoundTrip()
        {
            var point = new Point(10.5, 20.7);
            
            string geoJson = GeometryEngine.GeometryToGeoJson(point);
            var geometry = GeometryEngine.GeometryFromGeoJson(geoJson);

            Assert.IsType<Point>(geometry);
            var resultPoint = (Point)geometry;
            Assert.Equal(10.5, resultPoint.X, 5);
            Assert.Equal(20.7, resultPoint.Y, 5);
        }

        [Fact]
        public void TestGeometryEngineUnion()
        {
            var point1 = new Point(0, 0);
            var point2 = new Point(10, 10);

            var union = GeometryEngine.Union(point1, point2);

            Assert.NotNull(union);
            Assert.IsType<MultiPoint>(union);
        }

        [Fact]
        public void TestGeometryEngineIntersection()
        {
            var env1 = new Envelope(0, 0, 10, 10);
            var env2 = new Envelope(5, 5, 15, 15);

            var intersection = GeometryEngine.Intersection(env1, env2);

            Assert.NotNull(intersection);
            Assert.IsType<Envelope>(intersection);
            var result = (Envelope)intersection;
            Assert.Equal(5, result.XMin);
            Assert.Equal(5, result.YMin);
            Assert.Equal(10, result.XMax);
            Assert.Equal(10, result.YMax);
        }

        [Fact]
        public void TestGeometryEngineConvexHull()
        {
            var multiPoint = new MultiPoint();
            multiPoint.Add(new Point(0, 0));
            multiPoint.Add(new Point(10, 0));
            multiPoint.Add(new Point(5, 10));

            var hull = GeometryEngine.ConvexHull(multiPoint);

            Assert.NotNull(hull);
            Assert.IsType<Polygon>(hull);
        }
    }
}
