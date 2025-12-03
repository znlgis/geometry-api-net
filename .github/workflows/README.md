# GitHub Actions 自动发布 NuGet 包

本仓库已配置 GitHub Actions 工作流，可在创建新的 Git 标签（tag）时自动构建、测试并发布 NuGet 包到 NuGet.org。

## 工作流说明

工作流文件位于 `.github/workflows/publish-nuget.yml`。

### 触发条件

当推送符合 `v*.*.*` 格式的标签时触发，例如：
- `v1.0.0`
- `v2.1.3`
- `v1.0.0-beta`

### 工作流步骤

1. **检出代码** - 使用 `actions/checkout@v4` 检出仓库代码
2. **设置 .NET 环境** - 安装 .NET SDK 8.0.x
3. **提取版本号** - 从标签中提取版本号（去除 'v' 前缀）
4. **恢复依赖** - 运行 `dotnet restore`
5. **构建项目** - 使用 Release 配置构建
6. **运行测试** - 执行所有单元测试以确保质量
7. **打包 NuGet 包** - 为两个项目创建 NuGet 包：
   - `Esri.Geometry.Core`
   - `Esri.Geometry.Json`
8. **发布到 NuGet.org** - 推送包到 NuGet 仓库
9. **创建 GitHub Release** - 创建 GitHub 发布并附加 NuGet 包

## 使用方法

### 前置条件

1. **配置 NuGet API Key**
   
   您需要在 GitHub 仓库设置中添加一个名为 `NUGET_API_KEY` 的 Secret：
   
   a. 登录 [NuGet.org](https://www.nuget.org)
   
   b. 转到您的账户设置 → API Keys
   
   c. 创建一个新的 API Key，权限设置为 "Push" 和 "Push new packages and package versions"
   
   d. 在 GitHub 仓库中：
      - 转到 Settings → Secrets and variables → Actions
      - 点击 "New repository secret"
      - 名称：`NUGET_API_KEY`
      - 值：粘贴您的 NuGet API Key
      - 点击 "Add secret"

2. **确保项目配置正确**
   
   项目文件（.csproj）应包含必要的包元数据：
   - `<Authors>` - 作者信息
   - `<Description>` - 包描述
   - `<PackageLicenseExpression>` - 许可证信息

### 发布新版本

1. **确保代码已提交并推送到主分支**
   ```bash
   git add .
   git commit -m "Release version 1.0.0"
   git push origin main
   ```

2. **创建并推送标签**
   ```bash
   # 创建标签
   git tag v1.0.0
   
   # 推送标签到远程仓库
   git push origin v1.0.0
   ```

3. **监控工作流执行**
   
   - 转到 GitHub 仓库的 "Actions" 标签页
   - 您将看到 "Publish NuGet Packages" 工作流正在运行
   - 等待工作流完成（通常需要几分钟）

4. **验证发布**
   
   - 检查 [NuGet.org](https://www.nuget.org) 上的包是否已发布
   - 在 GitHub 的 "Releases" 页面查看新创建的发布

### 版本命名规范

建议遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范：

- **主版本号（MAJOR）**：当你做了不兼容的 API 修改
- **次版本号（MINOR）**：当你做了向下兼容的功能性新增
- **修订号（PATCH）**：当你做了向下兼容的问题修正

示例：
- `v1.0.0` - 第一个稳定版本
- `v1.1.0` - 添加新功能
- `v1.1.1` - 修复 bug
- `v2.0.0` - 重大更改（不向后兼容）

预发布版本：
- `v1.0.0-alpha`
- `v1.0.0-beta`
- `v1.0.0-rc1`

## 故障排除

### 工作流失败

1. **检查日志**
   - 在 GitHub Actions 页面点击失败的工作流
   - 查看详细日志以了解失败原因

2. **常见问题**
   
   - **NuGet API Key 无效**：检查 Secret 配置是否正确
   - **测试失败**：在本地运行 `dotnet test` 确保所有测试通过
   - **包已存在**：相同版本的包已在 NuGet.org 上发布，需要使用新版本号
   - **构建失败**：在本地运行 `dotnet build --configuration Release` 检查构建错误

### 重新发布

如果需要重新发布同一版本：

1. 从 GitHub 删除标签：
   ```bash
   git tag -d v1.0.0
   git push origin :refs/tags/v1.0.0
   ```

2. 从 NuGet.org 取消列出（unlist）现有包（无法完全删除）

3. 创建新标签重新发布

## 工作流文件说明

```yaml
name: Publish NuGet Packages

on:
  push:
    tags:
      - 'v*.*.*'  # 匹配版本标签
```

### 主要配置

- **运行环境**：`ubuntu-latest`
- **.NET 版本**：8.0.x
- **构建配置**：Release
- **输出目录**：`./packages`
- **NuGet 源**：https://api.nuget.org/v3/index.json

### 环境变量和 Secret

- `NUGET_API_KEY`：NuGet.org API 密钥（必需）
- `GITHUB_TOKEN`：自动提供，用于创建 GitHub Release

## 发布的包

此工作流会发布两个 NuGet 包：

1. **Esri.Geometry.Core** - 核心几何库
2. **Esri.Geometry.Json** - JSON 序列化支持

两个包使用相同的版本号，该版本号从 Git 标签提取。

## 最佳实践

1. **在本地测试**
   ```bash
   # 构建项目
   dotnet build --configuration Release
   
   # 运行测试
   dotnet test --configuration Release
   
   # 本地打包测试
   dotnet pack --configuration Release --output ./packages
   ```

2. **更新 CHANGELOG**
   在创建新版本之前，更新项目的 CHANGELOG 文件记录变更内容。

3. **使用发布分支**
   对于重要版本，考虑使用发布分支进行最终测试。

4. **保护主分支**
   配置分支保护规则，要求通过 PR 和测试后才能合并到主分支。

## 参考链接

- [GitHub Actions 文档](https://docs.github.com/en/actions)
- [NuGet 包发布指南](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [语义化版本规范](https://semver.org/lang/zh-CN/)
- [dotnet pack 命令](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
