#!/usr/bin/env bash
# run-tests.sh — geometry-api-net 全面测试入口
#   1) 单元测试（xunit，Release）
#   2) 真实数据 harness（控制台；数据无关：语料经 --data / GEOM_REAL_DATA_DIR，PostGIS 经 --pg / GEOM_TEST_PG_*）
#      - 缺语料 → 真实数据套件自动 Skip；缺 PG → 交叉验证套件自动 Skip
#      - 任一 FAIL → 退出码 1（CI 可直接断言）
# 用法:
#   ./run-tests.sh                 # 单测 + harness（自动探测 ./realdata 与 PG 环境变量）
#   ./run-tests.sh --with-harness  # 同上（显式）
#   GEOM_REAL_DATA_DIR=/path scripts/prep-data.sh 先生成语料
set -euo pipefail
cd "$(dirname "$0")"

CONFIG=Release
dotnet build -c "$CONFIG" --nologo -v q

echo "== 单元测试 =="
dotnet test --configuration "$CONFIG" --no-build --nologo

HARNESS_ARGS=()
if [ -d "${GEOM_REAL_DATA_DIR:-./realdata}" ]; then
  HARNESS_ARGS+=(--data "${GEOM_REAL_DATA_DIR:-./realdata}")
fi
if [ -n "${GEOM_TEST_PG_HOST:-}" ]; then
  HARNESS_ARGS+=(--pg)
fi

if [ ${#HARNESS_ARGS[@]} -gt 0 ]; then
  echo "== 真实数据 harness (${HARNESS_ARGS[*]}) =="
  dotnet run --no-build -c "$CONFIG" --project tools/OpenGIS.Esri.Geometry.RealDataHarness -- \
    "${HARNESS_ARGS[@]}" --sample "${GEOM_HARNESS_SAMPLE:-400}" "$@"
else
  echo "== 真实数据 harness：无语料且未配置 PG（跳过；用 scripts/prep-data.sh 生成语料）=="
fi
