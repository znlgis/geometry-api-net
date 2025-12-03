using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Tests.Geometries
{
    public class PolygonTests
    {
        [Fact]
        public void Polygon_DefaultConstructor_IsEmpty()
        {
            var polygon = new Polygon();
            Assert.True(polygon.IsEmpty);
        }

        [Fact]
        public void Polygon_Type_ReturnsPolygon()
        {
            var polygon = new Polygon();
            Assert.Equal(GeometryType.Polygon, polygon.Type);
        }

        [Fact]
        public void Polygon_Dimension_ReturnsTwo()
        {
            var polygon = new Polygon();
            Assert.Equal(2, polygon.Dimension);
        }

        [Fact]
        public void Polygon_AddRing_IncreasesRingCount()
        {
            var polygon = new Polygon();
            var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
            polygon.AddRing(ring);
            
            Assert.Equal(1, polygon.RingCount);
            Assert.False(polygon.IsEmpty);
        }

        [Fact]
        public void Polygon_Area_CalculatesCorrectly()
        {
            var polygon = new Polygon();
            var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0) };
            polygon.AddRing(ring);
            
            Assert.Equal(100, polygon.Area, 2);
        }

        [Fact]
        public void Polygon_GetRing_ReturnsCorrectRing()
        {
            var polygon = new Polygon();
            var ring = new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) };
            polygon.AddRing(ring);
            
            var retrievedRing = polygon.GetRing(0);
            Assert.Equal(3, retrievedRing.Count);
            Assert.Equal(0, retrievedRing[0].X);
            Assert.Equal(0, retrievedRing[0].Y);
        }

        [Fact]
        public void Polygon_GetEnvelope_ReturnsCorrectBounds()
        {
            var polygon = new Polygon();
            var ring = new[] { new Point(5, 5), new Point(15, 5), new Point(15, 15), new Point(5, 15), new Point(5, 5) };
            polygon.AddRing(ring);
            
            var envelope = polygon.GetEnvelope();
            Assert.Equal(5, envelope.XMin);
            Assert.Equal(5, envelope.YMin);
            Assert.Equal(15, envelope.XMax);
            Assert.Equal(15, envelope.YMax);
        }
    }
}
