using Esri.Geometry.Core.Geometries;

namespace Esri.Geometry.Tests.Geometries;

public class EnvelopeTests
{
  [Fact]
  public void Envelope_DefaultConstructor_IsEmpty()
  {
    var envelope = new Envelope();
    Assert.True(envelope.IsEmpty);
  }

  [Fact]
  public void Envelope_WithBounds_IsNotEmpty()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    Assert.False(envelope.IsEmpty);
  }

  [Fact]
  public void Envelope_Type_ReturnsEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    Assert.Equal(GeometryType.Envelope, envelope.Type);
  }

  [Fact]
  public void Envelope_Dimension_ReturnsTwo()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    Assert.Equal(2, envelope.Dimension);
  }

  [Fact]
  public void Envelope_Width_CalculatesCorrectly()
  {
    var envelope = new Envelope(0, 0, 10, 5);
    Assert.Equal(10, envelope.Width);
  }

  [Fact]
  public void Envelope_Height_CalculatesCorrectly()
  {
    var envelope = new Envelope(0, 0, 10, 5);
    Assert.Equal(5, envelope.Height);
  }

  [Fact]
  public void Envelope_Area_CalculatesCorrectly()
  {
    var envelope = new Envelope(0, 0, 10, 5);
    Assert.Equal(50, envelope.Area);
  }

  [Fact]
  public void Envelope_Center_CalculatesCorrectly()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var center = envelope.Center;

    Assert.Equal(5, center.X);
    Assert.Equal(5, center.Y);
  }

  [Fact]
  public void Envelope_Contains_PointInside_ReturnsTrue()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var point = new Point(5, 5);

    Assert.True(envelope.Contains(point));
  }

  [Fact]
  public void Envelope_Contains_PointOutside_ReturnsFalse()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    var point = new Point(15, 15);

    Assert.False(envelope.Contains(point));
  }

  [Fact]
  public void Envelope_Intersects_OverlappingEnvelope_ReturnsTrue()
  {
    var envelope1 = new Envelope(0, 0, 10, 10);
    var envelope2 = new Envelope(5, 5, 15, 15);

    Assert.True(envelope1.Intersects(envelope2));
  }

  [Fact]
  public void Envelope_Intersects_NonOverlappingEnvelope_ReturnsFalse()
  {
    var envelope1 = new Envelope(0, 0, 10, 10);
    var envelope2 = new Envelope(20, 20, 30, 30);

    Assert.False(envelope1.Intersects(envelope2));
  }

  [Fact]
  public void Envelope_Merge_Point_ExtendsEnvelope()
  {
    var envelope = new Envelope(0, 0, 10, 10);
    envelope.Merge(new Point(15, 15));

    Assert.Equal(0, envelope.XMin);
    Assert.Equal(0, envelope.YMin);
    Assert.Equal(15, envelope.XMax);
    Assert.Equal(15, envelope.YMax);
  }

  [Fact]
  public void Envelope_Merge_Envelope_ExtendsEnvelope()
  {
    var envelope1 = new Envelope(0, 0, 10, 10);
    var envelope2 = new Envelope(5, 5, 20, 20);
    envelope1.Merge(envelope2);

    Assert.Equal(0, envelope1.XMin);
    Assert.Equal(0, envelope1.YMin);
    Assert.Equal(20, envelope1.XMax);
    Assert.Equal(20, envelope1.YMax);
  }
}