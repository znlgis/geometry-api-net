using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Tests.Geometries
{
    public class MultiPointTests
    {
        [Fact]
        public void MultiPoint_DefaultConstructor_IsEmpty()
        {
            var multiPoint = new MultiPoint();
            Assert.True(multiPoint.IsEmpty);
        }

        [Fact]
        public void MultiPoint_Type_ReturnsMultiPoint()
        {
            var multiPoint = new MultiPoint();
            Assert.Equal(GeometryType.MultiPoint, multiPoint.Type);
        }

        [Fact]
        public void MultiPoint_Dimension_ReturnsZero()
        {
            var multiPoint = new MultiPoint();
            Assert.Equal(0, multiPoint.Dimension);
        }

        [Fact]
        public void MultiPoint_Add_IncreasesCount()
        {
            var multiPoint = new MultiPoint();
            multiPoint.Add(new Point(10, 20));
            
            Assert.Equal(1, multiPoint.Count);
            Assert.False(multiPoint.IsEmpty);
        }

        [Fact]
        public void MultiPoint_GetPoint_ReturnsCorrectPoint()
        {
            var multiPoint = new MultiPoint();
            var point = new Point(10, 20);
            multiPoint.Add(point);
            
            var retrieved = multiPoint.GetPoint(0);
            Assert.Equal(10, retrieved.X);
            Assert.Equal(20, retrieved.Y);
        }

        [Fact]
        public void MultiPoint_GetEnvelope_ReturnsCorrectBounds()
        {
            var multiPoint = new MultiPoint();
            multiPoint.Add(new Point(5, 5));
            multiPoint.Add(new Point(15, 10));
            multiPoint.Add(new Point(10, 15));
            
            var envelope = multiPoint.GetEnvelope();
            Assert.Equal(5, envelope.XMin);
            Assert.Equal(5, envelope.YMin);
            Assert.Equal(15, envelope.XMax);
            Assert.Equal(15, envelope.YMax);
        }

        [Fact]
        public void MultiPoint_ConstructorWithPoints_CreatesMultiPoint()
        {
            var points = new[] { new Point(10, 20), new Point(30, 40) };
            var multiPoint = new MultiPoint(points);
            
            Assert.Equal(2, multiPoint.Count);
            Assert.False(multiPoint.IsEmpty);
        }
    }
}
