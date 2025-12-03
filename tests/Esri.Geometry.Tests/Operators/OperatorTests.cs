using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

namespace Esri.Geometry.Tests.Operators
{
    public class OperatorTests
    {
        [Fact]
        public void DistanceOperator_PointToPoint_CalculatesCorrectDistance()
        {
            var point1 = new Point(0, 0);
            var point2 = new Point(3, 4);
            
            var distance = DistanceOperator.Instance.Execute(point1, point2);
            Assert.Equal(5, distance, 10);
        }

        [Fact]
        public void ContainsOperator_EnvelopeContainsPoint_ReturnsTrue()
        {
            var envelope = new Envelope(0, 0, 10, 10);
            var point = new Point(5, 5);
            
            var contains = ContainsOperator.Instance.Execute(envelope, point);
            Assert.True(contains);
        }

        [Fact]
        public void ContainsOperator_EnvelopeDoesNotContainPoint_ReturnsFalse()
        {
            var envelope = new Envelope(0, 0, 10, 10);
            var point = new Point(15, 15);
            
            var contains = ContainsOperator.Instance.Execute(envelope, point);
            Assert.False(contains);
        }

        [Fact]
        public void IntersectsOperator_OverlappingEnvelopes_ReturnsTrue()
        {
            var envelope1 = new Envelope(0, 0, 10, 10);
            var envelope2 = new Envelope(5, 5, 15, 15);
            
            var intersects = IntersectsOperator.Instance.Execute(envelope1, envelope2);
            Assert.True(intersects);
        }

        [Fact]
        public void IntersectsOperator_NonOverlappingEnvelopes_ReturnsFalse()
        {
            var envelope1 = new Envelope(0, 0, 10, 10);
            var envelope2 = new Envelope(20, 20, 30, 30);
            
            var intersects = IntersectsOperator.Instance.Execute(envelope1, envelope2);
            Assert.False(intersects);
        }
    }
}
