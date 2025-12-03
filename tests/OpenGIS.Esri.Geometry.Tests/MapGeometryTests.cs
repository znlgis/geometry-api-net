using OpenGIS.Esri.Geometry.Core;
using OpenGIS.Esri.Geometry.Core.Geometries;
using OpenGIS.Esri.Geometry.Core.SpatialReference;

namespace OpenGIS.Esri.Geometry.Tests;

public class MapGeometryTests
{
    [Fact]
    public void TestMapGeometryConstruction()
    {
        var point = new Point(10, 20);
        var sr = SpatialReference.Wgs84();
        var mapGeom = new MapGeometry(point, sr);

        Assert.NotNull(mapGeom.Geometry);
        Assert.NotNull(mapGeom.SpatialReference);
        Assert.Equal(point, mapGeom.Geometry);
        Assert.Equal(sr, mapGeom.SpatialReference);
    }

    [Fact]
    public void TestMapGeometryEmptyConstruction()
    {
        var mapGeom = new MapGeometry();

        Assert.Null(mapGeom.Geometry);
        Assert.Null(mapGeom.SpatialReference);
    }

    [Fact]
    public void TestMapGeometryEquality()
    {
        var point1 = new Point(10, 20);
        var point2 = new Point(10, 20);
        var sr1 = SpatialReference.Wgs84();
        var sr2 = SpatialReference.Wgs84();

        var mapGeom1 = new MapGeometry(point1, sr1);
        var mapGeom2 = new MapGeometry(point2, sr2);

        Assert.True(mapGeom1.Equals(mapGeom2));
        Assert.True(mapGeom1 == mapGeom2);
        Assert.False(mapGeom1 != mapGeom2);
    }

    [Fact]
    public void TestMapGeometryInequality()
    {
        var point1 = new Point(10, 20);
        var point2 = new Point(30, 40);
        var sr = SpatialReference.Wgs84();

        var mapGeom1 = new MapGeometry(point1, sr);
        var mapGeom2 = new MapGeometry(point2, sr);

        Assert.False(mapGeom1.Equals(mapGeom2));
        Assert.False(mapGeom1 == mapGeom2);
        Assert.True(mapGeom1 != mapGeom2);
    }

    [Fact]
    public void TestMapGeometryHashCode()
    {
        var point = new Point(10, 20);
        var sr = SpatialReference.Wgs84();
        var mapGeom1 = new MapGeometry(point, sr);
        var mapGeom2 = new MapGeometry(point, sr);

        Assert.Equal(mapGeom1.GetHashCode(), mapGeom2.GetHashCode());
    }

    [Fact]
    public void TestMapGeometryToString()
    {
        var point = new Point(10, 20);
        var sr = SpatialReference.Wgs84();
        var mapGeom = new MapGeometry(point, sr);

        var str = mapGeom.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    [Fact]
    public void TestMapGeometryWithNullGeometry()
    {
        var sr = SpatialReference.Wgs84();
        var mapGeom = new MapGeometry(null, sr);

        Assert.Null(mapGeom.Geometry);
        Assert.NotNull(mapGeom.SpatialReference);
    }

    [Fact]
    public void TestMapGeometryWithNullSpatialReference()
    {
        var point = new Point(10, 20);
        var mapGeom = new MapGeometry(point, null);

        Assert.NotNull(mapGeom.Geometry);
        Assert.Null(mapGeom.SpatialReference);
    }

    [Fact]
    public void TestMapGeometrySetters()
    {
        var mapGeom = new MapGeometry();
        var point = new Point(10, 20);
        var sr = SpatialReference.Wgs84();

        mapGeom.Geometry = point;
        mapGeom.SpatialReference = sr;

        Assert.Equal(point, mapGeom.Geometry);
        Assert.Equal(sr, mapGeom.SpatialReference);
    }
}