using System.Runtime.CompilerServices;

// 真实数据 harness 需访问内部关系引擎做单矩阵多谓词求值与性能统计。
[assembly: InternalsVisibleTo("OpenGIS.Esri.Geometry.RealDataHarness"), InternalsVisibleTo("dbg")]
