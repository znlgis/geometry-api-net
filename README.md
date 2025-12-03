# Esri Geometry API for .NET

[Esri Geometry API for Java](https://github.com/Esri/geometry-api-java) 的 C# 移植版本，针对 .NET Standard 2.0，为空间数据分析提供全面的几何操作功能。

## 概述

本库提供了一套完整的几何类型和空间操作，与 Esri 的几何模型兼容。设计用于跨平台使用，支持 .NET Core、.NET Framework 4.6.1+、Xamarin 以及其他 .NET Standard 2.0 兼容平台。

**注意**：所有源代码注释均为中文，便于中文开发者阅读和维护。

## 功能特性

### 几何类型
- **Point（点）** - 表示具有 X、Y 坐标的点（可选 Z 和 M 值）
- **MultiPoint（多点）** - 点的集合
- **Polyline（折线）** - 一条或多条连接的路径
- **Polygon（多边形）** - 一个或多个环形成的多边形
- **Envelope（包络）** - 轴对齐的边界矩形
- **Line（线段）** - 两点之间的线段

### 空间操作符

#### 空间关系操作符（9 个操作符）
- **Contains（包含）** - 测试一个几何对象是否包含另一个
- **Intersects（相交）** - 测试几何对象是否相交
- **Distance（距离）** - 计算几何对象之间的距离
- **Equals（相等）** - 测试两个几何对象在空间上是否相等
- **Disjoint（分离）** - 测试几何对象是否不相交
- **Within（内部）** - 测试 geometry1 是否在 geometry2 内部
- **Crosses（交叉）** - 测试几何对象是否交叉
- **Touches（相接）** - 测试几何对象是否在边界处相接
- **Overlaps（重叠）** - 测试相同维度的几何对象是否重叠

#### 几何操作
- **Buffer（缓冲区）** - 在几何对象周围创建缓冲区（偏移多边形）
- **ConvexHull（凸包）** - 使用 Graham 扫描算法计算凸包
- **Area（面积）** - 计算多边形和包络的面积
- **Length（长度）** - 计算几何对象的长度/周长

#### 几何辅助方法（新增！）
- **CalculateArea2D()** - 直接计算几何对象的 2D 面积
- **CalculateLength2D()** - 直接计算几何对象的 2D 长度/周长
- **Copy()** - 创建包含所有属性的几何对象深层副本
- **IsValid()** - 检查几何对象是否有效（非 null 且非空）
- **IsPoint, IsLinear, IsArea** - 几何对象类型检查属性

#### 集合操作（4 个操作符）
- **Union（并集）** - 合并两个几何对象（任一几何对象中的所有点）
- **Intersection（交集）** - 查找公共区域（两个几何对象中的所有点）
- **Difference（差集）** - 从一个几何对象中减去另一个（geometry1 - geometry2）
- **SymmetricDifference（对称差）** - 查找排他区域（(A-B) ∪ (B-A)）

#### 其他操作符（6 个操作符）
- **Simplify（简化）** - 使用 Douglas-Peucker 算法简化几何对象
- **SimplifyOGC（OGC 简化）** - 根据 OGC 规范简化几何对象（新增！）
- **Centroid（质心）** - 计算几何对象的质心
- **Boundary（边界）** - 根据 OGC 规范计算边界
- **Generalize（概化）** - 删除顶点的同时保持总体形状
- **Densify（密化）** - 添加顶点以确保没有线段超过最大长度

#### 高级操作符
- **Clip（裁剪）** - 使用 Cohen-Sutherland 算法将几何对象裁剪到包络
- **GeodesicDistance（大地测量距离）** - 计算 WGS84 椭球上的大圆距离（Vincenty 公式）
- **GeodesicArea（大地测量面积）** - 使用球面过量公式计算 WGS84 椭球上的大地测量面积
- **Offset（偏移）** - 在指定距离处创建偏移曲线/多边形（垂直位移）
- **Proximity2D（2D 邻近）** - 查找几何对象上的最近坐标和顶点（GetNearestCoordinate、GetNearestVertex、GetNearestVertices）

#### 便利 API
- **GeometryEngine** - 使用便利方法包装所有操作符的简化静态 API
- **MapGeometry** - 将几何对象与空间参考捆绑（新增！）

### 导入/导出格式
- **WKT (Well-Known Text)** - 对所有几何类型的完整导入和导出支持
- **WKB (Well-Known Binary)** - 支持字节序的二进制格式导入/导出
- **GeoJSON** - 对所有几何类型的完整 GeoJSON 导入/导出
- **Esri JSON** - Esri 特定的 JSON 格式导入/导出（x/y 属性 vs GeoJSON 坐标数组）
- **JSON** - 使用 System.Text.Json 的 Point 序列化

### 空间参考系统
- 支持众所周知的 ID（WKID）
- 内置支持 WGS 84（EPSG:4326）和 Web Mercator（EPSG:3857）

### JSON 序列化
- Point 几何对象的 System.Text.Json 转换器
- 支持 X、Y、Z 和 M 坐标

## 快速开始

### 安装

#### 通过 NuGet 安装（推荐）

```bash
# 安装核心库
dotnet add package Esri.Geometry.Core

# 安装 JSON 支持
dotnet add package Esri.Geometry.Json
```

或在 .csproj 文件中添加：

```xml
<ItemGroup>
  <PackageReference Include="Esri.Geometry.Core" Version="*" />
  <PackageReference Include="Esri.Geometry.Json" Version="*" />
</ItemGroup>
```

**提示**: 将 `Version="*"` 替换为具体的版本号，如 `Version="1.0.0"`。

#### 通过项目引用安装

在项目中添加对核心库的引用：

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Esri.Geometry.Core/Esri.Geometry.Core.csproj" />
</ItemGroup>
```

对于 JSON 支持，还需引用：

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Esri.Geometry.Json/Esri.Geometry.Json.csproj" />
</ItemGroup>
```

### 基本用法

```csharp
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

// 创建点
var point1 = new Point(10, 20);
var point2 = new Point(30, 40);

// 计算距离
var distance = point1.Distance(point2);
Console.WriteLine($"Distance: {distance}");

// 创建包络
var envelope = new Envelope(0, 0, 100, 100);

// 测试包含关系
var testPoint = new Point(50, 50);
bool contains = envelope.Contains(testPoint);
Console.WriteLine($"Contains: {contains}");

// 使用操作符
var distanceOp = DistanceOperator.Instance;
var dist = distanceOp.Execute(point1, point2);

var containsOp = ContainsOperator.Instance;
var result = containsOp.Execute(envelope, testPoint);
```

### GeometryEngine - 简化的 API

`GeometryEngine` 类为所有几何操作提供了简化的静态 API：

```csharp
using Esri.Geometry.Core;
using Esri.Geometry.Core.Geometries;

// 所有操作都可作为静态方法使用
var point1 = new Point(0, 0);
var point2 = new Point(3, 4);

// 空间关系
bool contains = GeometryEngine.Contains(envelope, point);
bool intersects = GeometryEngine.Intersects(geom1, geom2);
double distance = GeometryEngine.Distance(point1, point2);  // 5.0

// 集合操作
var union = GeometryEngine.Union(geom1, geom2);
var intersection = GeometryEngine.Intersection(geom1, geom2);
var difference = GeometryEngine.Difference(geom1, geom2);

// 几何操作
var buffer = GeometryEngine.Buffer(point, 10.0);
var hull = GeometryEngine.ConvexHull(multiPoint);
double area = GeometryEngine.Area(polygon);
double length = GeometryEngine.Length(polyline);

// 导入/导出
string wkt = GeometryEngine.GeometryToWkt(geometry);
var geom = GeometryEngine.GeometryFromWkt(wkt);

string geoJson = GeometryEngine.GeometryToGeoJson(geometry);
var geom2 = GeometryEngine.GeometryFromGeoJson(geoJson);

// 邻近操作
var result = GeometryEngine.GetNearestCoordinate(geometry, queryPoint);
Console.WriteLine($"最近的点: ({result.Coordinate.X}, {result.Coordinate.Y})");
Console.WriteLine($"距离: {result.Distance}");
```

### 使用多边形

```csharp
var polygon = new Polygon();
var ring = new[] {
    new Point(0, 0),
    new Point(10, 0),
    new Point(10, 10),
    new Point(0, 10),
    new Point(0, 0)
};
polygon.AddRing(ring);

var area = polygon.Area;
Console.WriteLine($"多边形面积: {area}");
```

### 空间参考

```csharp
using Esri.Geometry.Core.SpatialReference;

// 创建 WGS 84 空间参考
var wgs84 = SpatialReference.Wgs84();
Console.WriteLine($"WKID: {wgs84.Wkid}");

// 创建 Web Mercator 空间参考
var webMercator = SpatialReference.WebMercator();
```

### WKT 导入/导出

```csharp
using Esri.Geometry.Core.IO;

// 导出为 WKT
var point = new Point(10.5, 20.7);
var wkt = WktExportOperator.ExportToWkt(point);
// 结果: "POINT (10.5 20.7)"

// 从 WKT 导入
var geometry = WktImportOperator.ImportFromWkt("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
var polygon = (Polygon)geometry;
```

### 空间关系测试

```csharp
// 测试各种空间关系
var env1 = new Envelope(0, 0, 10, 10);
var env2 = new Envelope(5, 5, 15, 15);

bool intersects = IntersectsOperator.Instance.Execute(env1, env2);  // true
bool overlaps = OverlapsOperator.Instance.Execute(env1, env2);      // true
bool disjoint = DisjointOperator.Instance.Execute(env1, env2);      // false
bool equals = EqualsOperator.Instance.Execute(env1, env1);          // true
```

### 几何操作

```csharp
// 在点周围创建缓冲区
var point = new Point(10, 20);
var buffer = BufferOperator.Instance.Execute(point, 5.0);

// 计算凸包
var multiPoint = new MultiPoint();
multiPoint.Add(new Point(0, 0));
multiPoint.Add(new Point(10, 0));
multiPoint.Add(new Point(5, 10));
var hull = ConvexHullOperator.Instance.Execute(multiPoint);

// 计算面积和长度
var polygon = new Polygon();
// ... 向多边形添加环
double area = AreaOperator.Instance.Execute(polygon);
double perimeter = LengthOperator.Instance.Execute(polygon);

// 简化折线
var polyline = new Polyline();
// ... 添加路径
var simplified = SimplifyOperator.Instance.Execute(polyline, tolerance: 0.5);

// 计算质心
var centroid = CentroidOperator.Instance.Execute(polygon);

// 获取边界
var boundary = BoundaryOperator.Instance.Execute(polygon); // 返回 Polyline

// 将几何对象裁剪到包络
var clipEnvelope = new Envelope(0, 0, 100, 100);
var clipped = ClipOperator.Instance.Execute(polyline, clipEnvelope);

// 计算大地测量距离（地球上的大圆）
var newYork = new Point(-74.0060, 40.7128);  // 经度，纬度
var london = new Point(-0.1278, 51.5074);
double distanceMeters = GeodesicDistanceOperator.Instance.Execute(newYork, london);
// 结果: ~5,570,000 米（5,570 公里）

// 概化 - 删除顶点的同时保持形状
var generalizedPolyline = GeneralizeOperator.Instance.Execute(polyline, maxDeviation: 0.5);

// 密化 - 添加顶点以确保没有线段超过最大长度
var densifiedPolyline = DensifyOperator.Instance.Execute(polyline, maxSegmentLength: 5.0);
```

### 集合操作

```csharp
using Esri.Geometry.Core.Operators;

// 并集 - 合并几何对象
var point1 = new Point(0, 0);
var point2 = new Point(10, 10);
var union = UnionOperator.Instance.Execute(point1, point2); // 返回 MultiPoint

var env1 = new Envelope(0, 0, 10, 10);
var env2 = new Envelope(5, 5, 15, 15);
var envUnion = UnionOperator.Instance.Execute(env1, env2); // 返回 Envelope(0, 0, 15, 15)

// 交集 - 查找公共区域
var intersection = IntersectionOperator.Instance.Execute(env1, env2); // 返回 Envelope(5, 5, 10, 10)

var testPoint = new Point(7, 7);
var ptIntersection = IntersectionOperator.Instance.Execute(testPoint, env1); // 返回 Point(7, 7)

// 差集 - 从一个几何对象中减去另一个
var mp = new MultiPoint();
mp.Add(new Point(2, 2));
mp.Add(new Point(5, 5));
mp.Add(new Point(12, 12));

var difference = DifferenceOperator.Instance.Execute(mp, env1); // 返回 Point(12, 12) - env1 外的点

// 对称差 - 任一中的点但不同时在两者中
var mp1 = new MultiPoint();
mp1.Add(new Point(0, 0));
mp1.Add(new Point(10, 10));

var mp2 = new MultiPoint();
mp2.Add(new Point(10, 10));
mp2.Add(new Point(20, 20));

var symDiff = SymmetricDifferenceOperator.Instance.Execute(mp1, mp2); // 返回包含 (0,0) 和 (20,20) 的 MultiPoint
```

### WKB 导入/导出

```csharp
using Esri.Geometry.Core.IO;

// 导出为 WKB（二进制格式）
var point = new Point(10.5, 20.7);
byte[] wkb = WkbExportOperator.ExportToWkb(point);

// 使用大端字节序导出
byte[] wkbBigEndian = WkbExportOperator.ExportToWkb(point, bigEndian: true);

// 从 WKB 导入
var geometry = WkbImportOperator.ImportFromWkb(wkb);
var parsedPoint = (Point)geometry;
```

### GeoJSON 导入/导出

```csharp
using Esri.Geometry.Core.IO;

// 导出为 GeoJSON
var point = new Point(10.5, 20.3, 30.7);
string geoJson = GeoJsonExportOperator.ExportToGeoJson(point);
// 结果: {"type":"Point","coordinates":[10.5,20.3,30.7]}

// 导出 MultiPoint
var multiPoint = new MultiPoint();
multiPoint.Add(new Point(10, 20));
multiPoint.Add(new Point(30, 40));
string mpGeoJson = GeoJsonExportOperator.ExportToGeoJson(multiPoint);
// 结果: {"type":"MultiPoint","coordinates":[[10,20],[30,40]]}

// 导出 Polyline（单路径变为 LineString）
var polyline = new Polyline();
polyline.AddPath(new List<Point> { new Point(0, 0), new Point(10, 10) });
string lineGeoJson = GeoJsonExportOperator.ExportToGeoJson(polyline);
// 结果: {"type":"LineString","coordinates":[[0,0],[10,10]]}

// 导出 Polygon
var polygon = new Polygon();
polygon.AddRing(new List<Point> 
{ 
    new Point(0, 0), 
    new Point(10, 0), 
    new Point(10, 10), 
    new Point(0, 10), 
    new Point(0, 0) 
});
string polygonGeoJson = GeoJsonExportOperator.ExportToGeoJson(polygon);
// 结果: {"type":"Polygon","coordinates":[[[0,0],[10,0],[10,10],[0,10],[0,0]]]}

// 从 GeoJSON 导入
var importedGeometry = GeoJsonImportOperator.ImportFromGeoJson(geoJson);
var importedPoint = (Point)importedGeometry;
```

### 邻近操作

```csharp
using Esri.Geometry.Core;
using Esri.Geometry.Core.Geometries;
using Esri.Geometry.Core.Operators;

// 查找几何对象上最接近查询点的坐标
var polyline = new Polyline();
polyline.AddPath(new[] {
    new Point(0, 0),
    new Point(10, 0),
    new Point(10, 10)
});

var queryPoint = new Point(5, 5);

// 使用 GeometryEngine（推荐）
var result = GeometryEngine.GetNearestCoordinate(polyline, queryPoint);
Console.WriteLine($"最近的坐标: ({result.Coordinate.X}, {result.Coordinate.Y})");
Console.WriteLine($"距离: {result.Distance}");
Console.WriteLine($"顶点索引: {result.VertexIndex}");

// 直接使用操作符
var proximity = Proximity2DOperator.Instance;
var result2 = proximity.GetNearestVertex(polyline, queryPoint);
Console.WriteLine($"最近的顶点: ({result2.Coordinate.X}, {result2.Coordinate.Y})");

// 在搜索半径内查找多个顶点
var multiPoint = new MultiPoint();
multiPoint.Add(new Point(0, 0));
multiPoint.Add(new Point(10, 10));
multiPoint.Add(new Point(20, 20));

var results = GeometryEngine.GetNearestVertices(
    multiPoint, 
    queryPoint,
    searchRadius: 15.0,
    maxVertexCount: 5
);

foreach (var r in results)
{
    Console.WriteLine($"点: ({r.Coordinate.X}, {r.Coordinate.Y}), 距离: {r.Distance}");
}

// 测试点是否在多边形内部
var polygon = new Polygon();
polygon.AddRing(new[] {
    new Point(0, 0),
    new Point(100, 0),
    new Point(100, 100),
    new Point(0, 100),
    new Point(0, 0)
});

var testPoint = new Point(50, 50);
var nearestResult = GeometryEngine.GetNearestCoordinate(polygon, testPoint, testPolygonInterior: true);

if (nearestResult.Distance == 0)
{
    Console.WriteLine("点在多边形内部");
}
```

## 项目结构

```
Esri.Geometry.Api/
├── src/
│   ├── Esri.Geometry.Core/          # 核心几何库
│   │   ├── Geometries/               # 几何类型
│   │   ├── Operators/                # 几何操作符
│   │   ├── SpatialReference/         # 空间参考系统
│   │   └── IO/                       # WKT 和 WKB 导入/导出
│   └── Esri.Geometry.Json/           # JSON 序列化支持
├── tests/
│   └── Esri.Geometry.Tests/          # 单元测试（xUnit）
└── samples/
    └── Esri.Geometry.Samples/        # 示例应用程序
```

## 构建项目

### 前提条件
- .NET SDK 8.0 或更高版本（用于构建）
- 该库目标为 .NET Standard 2.0 以实现最大兼容性

### 构建命令

```bash
# 还原依赖项
dotnet restore

# 构建所有项目
dotnet build

# 运行测试
dotnet test

# 运行示例应用程序
cd samples/Esri.Geometry.Samples
dotnet run
```

## 测试

项目使用 xUnit 进行单元测试。测试位于 `tests/Esri.Geometry.Tests` 目录中。

```bash
# 运行所有测试
dotnet test

# 使用详细输出运行测试
dotnet test --logger "console;verbosity=detailed"
```

## 技术栈

- **目标框架**: .NET Standard 2.0
- **语言**: C# 7.0+
- **JSON 库**: System.Text.Json 8.0.6
- **测试框架**: xUnit
- **许可证**: LGPL 2.1

## 路线图

### 已完成的功能 ✅
- [x] 全部 9 个空间关系操作符（Contains、Intersects、Distance、Equals、Disjoint、Within、Crosses、Touches、Overlaps）
- [x] WKT（Well-Known Text）对所有几何类型的导入/导出
- [x] WKB（Well-Known Binary）导入/导出，支持字节序
- [x] Buffer 操作符（简化的正方形/矩形缓冲区）
- [x] 凸包计算（Graham 扫描算法）
- [x] 面积和长度计算操作符
- [x] Simplify 操作符（Douglas-Peucker 算法）
- [x] 质心计算（质量中心）
- [x] 边界计算（OGC 规范）
- [x] Clip 操作符（Cohen-Sutherland 线段裁剪）
- [x] 大地测量距离（WGS84 椭球上的 Vincenty 公式）
- [x] 集合操作（Union、Intersection、Difference、SymmetricDifference）
- [x] Generalize 操作符（删除顶点的同时保持形状）
- [x] Densify 操作符（向线段添加顶点）
- [x] Proximity2D 操作符（查找最近的坐标和顶点）
- [x] GeometryEngine 便利类（简化的静态 API）
- [x] 使用光线投射算法的点在多边形内测试

### 测试覆盖率
- **255 个测试通过**，具有全面的覆盖率
- 28 个几何类型测试
- 14 个空间关系操作符测试
- 23 个附加操作符测试（Simplify、Centroid、Boundary、Generalize、Densify）
- 12 个几何操作测试（Buffer、ConvexHull、Area、Length）
- 20 个高级操作符测试（Clip、GeodesicDistance、GeodesicArea、Offset、Esri JSON）
- 24 个邻近和 GeometryEngine 测试
- 23 个集合操作测试（Union、Intersection、Difference、SymmetricDifference）
- 17 个 WKT 导入/导出测试
- 10 个 WKB 导入/导出测试
- 8 个 GeoJSON 导入/导出测试
- 4 个 JSON 序列化测试
- 10 个 MapGeometry 测试
- 18 个 SimplifyOGC 操作符测试
- 17 个几何辅助方法测试（新增！）

### 计划中的功能
- [ ] Cut 操作符（使用折线切割几何对象 - 需要复杂的拓扑算法）
- [ ] 投影/转换支持（需要外部投影库）
- [ ] Relate 操作符（DE-9IM 空间关系 - 需要复杂的拓扑计算）
- [ ] 使用 Span<T> 和 Memory<T> 的性能优化
- [ ] 圆形缓冲区生成（目前仅支持正方形/矩形）
- [ ] Union/Intersection/Difference 的完整多边形裁剪（目前对复杂多边形进行了简化）

### 最近实现的功能 ✅
- [x] Offset 操作符（创建偏移曲线/多边形）
- [x] ESRI JSON 导入/导出（Esri 专有格式）
- [x] 大地测量面积计算（WGS84 椭球）
- [x] **GeometryEngine** - 所有操作符的简化静态 API
- [x] **Proximity2D 操作符** - 查找最近的坐标和顶点
- [x] **点在多边形内测试** - Contains 操作符的光线投射算法
- [x] **SimplifyOGC 操作符** - 符合 OGC 标准的几何简化
- [x] **MapGeometry** - 将几何对象与空间参考捆绑
- [x] **几何辅助方法** - CalculateArea2D、CalculateLength2D、Copy、IsValid（新增！）

## 代码注释

本项目的所有源代码注释均已翻译为中文，包括：
- 所有几何类型的 XML 文档注释
- 所有操作符的完整注释
- 接口和辅助类的注释
- 方法参数和返回值的说明

这使得中文开发者能够更容易地理解和使用该库。

## 发布和版本管理

本项目使用 GitHub Actions 自动发布 NuGet 包。当创建新的版本标签（tag）时，会自动构建、测试并发布包到 NuGet.org。

### 自动发布流程

1. 创建新的版本标签（例如：`v1.0.0`）
2. GitHub Actions 自动触发发布工作流
3. 构建项目并运行所有测试
4. 打包两个 NuGet 包：`Esri.Geometry.Core` 和 `Esri.Geometry.Json`
5. 发布到 NuGet.org
6. 创建 GitHub Release

### 发布新版本

```bash
# 创建版本标签
git tag v1.0.0

# 推送标签到远程仓库触发自动发布
git push origin v1.0.0
```

详细的发布说明和配置，请参阅 [GitHub Actions 工作流文档](.github/workflows/README.md)。


## 贡献

欢迎贡献！请随时提交问题和拉取请求。

## 许可证

本项目采用 GNU Lesser General Public License v2.1 许可 - 有关详细信息，请参阅 [LICENSE](LICENSE) 文件。

## 致谢

本项目是 [Esri Geometry API for Java](https://github.com/Esri/geometry-api-java) 的 C# 移植版本，Java 版本采用 Apache 2.0 许可证。

## 相关项目

- [Esri Geometry API for Java](https://github.com/Esri/geometry-api-java) - 原始 Java 实现
- [geometry-api-cs](https://github.com/Esri/geometry-api-cs) - Esri 的另一个 .NET 实现

## 支持

如有问题、疑问或贡献，请访问 [GitHub 仓库](https://github.com/znlgis/geometry-api-net)。
