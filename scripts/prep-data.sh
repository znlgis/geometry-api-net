#!/usr/bin/env bash
# prep-data.sh — 下载 Natural Earth 10m 语料并转为 harness 可发现的 GeoJSONSeq/GeoJSON。
# 用法: scripts/prep-data.sh [目标目录]   （默认 ./realdata；harness 经 --data 或 GEOM_REAL_DATA_DIR 注入）
# 依赖: curl、unzip、ogr2ogr（OSGeo4W / GDAL ≥ 3）。离线环境请预先准备语料目录，harness 会自动跳过相关套件。
set -euo pipefail

OUT="${1:-./realdata}"
mkdir -p "$OUT/raw" "$OUT/unz" "$OUT/geojsonl"

LAYERS=(
  "10m/cultural/ne_10m_admin_0_countries"
  "10m/physical/ne_10m_lakes"
  "10m/physical/ne_10m_rivers_lake_centerlines"
  "10m/physical/ne_10m_glaciated_areas"
  "10m/cultural/ne_10m_populated_places"
)

for L in "${LAYERS[@]}"; do
  N=$(basename "$L")
  [ -s "$OUT/geojsonl/$N.jsonl" ] && { echo "skip $N (exists)"; continue; }
  echo "fetch $N ..."
  curl -sfL -o "$OUT/raw/$N.zip" "https://naciscdn.org/naturalearth/$L.zip"
  mkdir -p "$OUT/unz/$N"
  unzip -qo "$OUT/raw/$N.zip" -d "$OUT/unz/$N"
  ogr2ogr -f GeoJSONSeq "$OUT/geojsonl/$N.jsonl" "$OUT/unz/$N/$N.shp"
done

echo "语料就绪: $OUT"
