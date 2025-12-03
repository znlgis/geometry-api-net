using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Tests.Geometries
{
    public class PointTests
    {
        [Fact]
        public void Point_DefaultConstructor_IsEmpty()
        {
            var point = new Point();
            Assert.True(point.IsEmpty);
        }

        [Fact]
        public void Point_WithCoordinates_IsNotEmpty()
        {
            var point = new Point(10, 20);
            Assert.False(point.IsEmpty);
            Assert.Equal(10, point.X);
            Assert.Equal(20, point.Y);
        }

        [Fact]
        public void Point_Type_ReturnsPoint()
        {
            var point = new Point(10, 20);
            Assert.Equal(GeometryType.Point, point.Type);
        }

        [Fact]
        public void Point_Dimension_ReturnsZero()
        {
            var point = new Point(10, 20);
            Assert.Equal(0, point.Dimension);
        }

        [Fact]
        public void Point_GetEnvelope_ReturnsCorrectEnvelope()
        {
            var point = new Point(10, 20);
            var envelope = point.GetEnvelope();
            
            Assert.Equal(10, envelope.XMin);
            Assert.Equal(20, envelope.YMin);
            Assert.Equal(10, envelope.XMax);
            Assert.Equal(20, envelope.YMax);
        }

        [Fact]
        public void Point_Distance_CalculatesCorrectDistance()
        {
            var point1 = new Point(0, 0);
            var point2 = new Point(3, 4);
            
            var distance = point1.Distance(point2);
            Assert.Equal(5, distance, 10);
        }

        [Fact]
        public void Point_WithZCoordinate_StoresZ()
        {
            var point = new Point(10, 20, 30);
            Assert.Equal(30, point.Z);
        }

        [Fact]
        public void Point_Equals_WithSameCoordinates_ReturnsTrue()
        {
            var point1 = new Point(10, 20);
            var point2 = new Point(10, 20);
            
            Assert.True(point1.Equals(point2));
        }

        [Fact]
        public void Point_Equals_WithDifferentCoordinates_ReturnsFalse()
        {
            var point1 = new Point(10, 20);
            var point2 = new Point(10, 21);
            
            Assert.False(point1.Equals(point2));
        }
    }
}
