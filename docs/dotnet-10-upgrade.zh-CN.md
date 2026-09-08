# .NET 10 升级记录与复测说明

日期：2026-09-08。分支：`feature/Dot_Net_Update`。

## 结果

后端已由 .NET 8 升级到 .NET 10 LTS，Microsoft 配套包统一为稳定版 10.0.11。
本机编译、前端构建、真实 HTTP 接口与 LocalDB 测试、发布包静态资源测试通过。
未修改现有数据库，未部署到远程服务器，未自动提交或推送 Git。

## 实际执行步骤

1. 检查当前分支与工作区，确认升级前没有未提交修改。
2. 检查后端项目、启动配置、前端构建配置、控制器、业务服务与数据库模型。
3. 编译原 .NET 8 解决方案：成功，0 个错误，6 条原有空值警告。
4. 将 `OtakuQuest/OtakuQuest.Server/OtakuQuest.Server.csproj` 的 `TargetFramework` 从 `net8.0` 改成 `net10.0`。
5. 将下列 NuGet 依赖更新为 `10.0.11`：
   - `Microsoft.AspNetCore.Authentication.JwtBearer`（原 8.0.24）
   - `Microsoft.AspNetCore.SpaProxy`（原 `8.*-*` 浮动范围）
   - `Microsoft.EntityFrameworkCore.SqlServer`（原 8.0.24）
   - `Microsoft.EntityFrameworkCore.Tools`（原 8.0.24）
6. 保留 BCrypt.Net-Next 4.1.0 与 Swashbuckle.AspNetCore 6.6.2；本次密码验证、Swagger JSON 和 Swagger UI 测试均通过，无须为了升级框架而同时迁移这两套 API。
7. 新增根目录 `global.json`：要求稳定 .NET 10 SDK，最低 10.0.100，`latestFeature` 允许使用本机较新的 10.0 功能带，禁止预览 SDK，不会自动选择 .NET 11。
8. 还原依赖并编译升级后的解决方案；单独执行 TypeScript 检查与 Vite 生产构建。
9. 新增 `tests/UpgradeSmoke` 可执行集成测试，使用真实 Kestrel HTTP 服务与随机名称的独立 SQL Server LocalDB 数据库。
10. 检查模型与旧迁移快照：一致；在独立库执行全部已有迁移成功，再次执行无新增迁移。因此本次没有新增迁移、没有更改旧迁移文件或手改快照版本。
11. 执行本地发布。首次发现发布命令成功，但发布目录缺少前端静态文件，访问 `/` 返回 404。
12. 在后端项目补充 `PublishClientAssets` 目标，将前端项目生成的 `dist` 加入发布目录 `wwwroot`。前端输出缺失时明确报错。重新发布后首页与 JavaScript 文件均可通过 HTTP 获取。
13. 对最终发布包重跑业务回归，共 39 项检查通过；前端 HTTPS 开发服务另有 2 项检查通过。
14. 更新 README 中框架、SDK、IDE、前端路径和开发地址说明，并补充本文。

首次沙箱编译无法读取用户 NuGet.Config，前端 esbuild 也受到目录访问限制；在正常用户环境执行后成功。这两次属于执行环境权限问题，不是项目升级兼容性错误。

## 验证环境与结果

- Windows，.NET SDK 10.0.400，.NET / ASP.NET Core Runtime 10.0.11。
- Node.js 22.20.0，npm 10.9.3。
- SQL Server LocalDB 默认实例 `(localdb)\MSSQLLocalDB`。

| 检查 | 结果 |
| --- | --- |
| 升级前 Release 编译 | 成功；6 条警告，0 错误 |
| 升级后 Release 编译 | 成功；同样 6 条警告，0 错误 |
| TypeScript + Vite 生产构建 | 成功 |
| 普通后端集成检查 | 36 项通过 |
| 最终发布包集成检查 | 39 项通过（包含上述业务检查与 3 项静态资源检查） |
| Vite HTTPS 开发服务 | React 首页与 main.tsx 转换/响应，2 项通过 |
| 本地 publish | 成功，包含 wwwroot 前端文件 |

接口检查覆盖：

- Swagger 文档生成、Swagger UI 可访问。
- 未登录与无效 JWT 返回 401，空登录参数返回 400。
- 注册成功、重复注册拒绝、错误密码拒绝、正确登录返回 JWT。
- JWT 正确识别玩家，玩家资料查询正常。
- 创建任务、查询已保存任务、完成任务、奖励写入玩家资料、拒绝重复完成。
- 创建商品、商店查询、购买、装备、背包持久化、资料中的装备名称更新。
- 创建测试 Boss、分配当前 Boss、攻击并击败 Boss。
- SQL Server 中测试用户和任务数据符合预期。
- 发布包提供 React 首页和正确类型的 JavaScript 资源。

测试只使用 `OtakuQuest_UpgradeSmoke_<随机 GUID>` 数据库，连接字符串与 JWT 配置通过子进程环境变量覆盖；测试不会读取业务数据库连接用于迁移或清理。成功或失败均进入清理逻辑，关闭测试服务并删除本次测试库。已完成的测试库已清理。

## 如何复测

以下命令在仓库根目录 `Otaku-quest_onlab` 执行。首次检出需安装 .NET 10 SDK、Node 与 LocalDB，并在前端目录执行 `npm ci`。

```powershell
dotnet --version
dotnet build OtakuQuest/OtakuQuest.sln -c Release

Push-Location OtakuQuest/otakuquest.client
npm run build
Pop-Location

dotnet run --project tests/UpgradeSmoke/UpgradeSmoke.csproj -c Release
powershell -NoProfile -File tests/Test-Frontend.ps1

dotnet publish OtakuQuest/OtakuQuest.Server/OtakuQuest.Server.csproj -c Release --no-restore -o ../artifacts/net10-publish
dotnet run --project tests/UpgradeSmoke/UpgradeSmoke.csproj -c Release -- ../artifacts/net10-publish
```

集成测试是控制台测试程序，失败返回非零退出码；不要用 `dotnet test` 代替上面的命令。普通模式固定验证 Release 构建。传入发布目录时应先重新 publish，避免验证旧产物。

前端开发检查脚本使用随机本地端口，启动隐藏 Vite 进程并在结束时关闭。它使用 `curl --insecure` 检查本地开发 HTTPS 内容，所以不代表浏览器已经信任开发证书。

## 日常启动

使用支持 .NET 10 的 Visual Studio 2026，或在仓库根目录执行：

```powershell
dotnet run --project OtakuQuest/OtakuQuest.Server/OtakuQuest.Server.csproj --launch-profile https
```

后端通常为 `https://localhost:7048`，Swagger 为该地址下的 `/swagger`。前端使用 `https://localhost:1436`；也可以在前端目录单独执行 `npm run dev`。本地 `.env` 的 `VITE_API_BASE_URL` 应指向实际后端地址。Vite 在构建时写入此值，部署到另一地址时应使用对应环境配置重新构建。

## 已知限制与原有警告

- 未进行逐页面浏览器点击测试，也未验证 Visual Studio F5/SpaProxy 自动拉起的完整交互流程；本次已单独验证前端 HTTPS 服务、后端接口与发布包资源。
- 业务测试使用隔离库与新建测试账号，没有复制现有业务数据。现有数据库的数据特例、远程 SQL Server、正式域名、IIS、反向代理和线上 HTTPS 配置尚未验证。
- 原有 6 条 C# 空值警告仍保留：AuthResponseDto 两条、CompleteTaskResponseDto 一条、PlayerStatsDto 一条、BossService 两条。升级前后相同，本次不改变业务空值语义。
- 未升级前端依赖，未进行完整依赖漏洞审计。
- CLI `dotnet-ef` 是独立工具；若使用它执行迁移，需自行确认工具为匹配的 10.0.x 版本。此次测试直接调用 EF 迁移 API，没有修改全局工具。

## 部署与回退

Azure 流程使用 .NET 10 构建并在 PR 中检查。main 分支收到 push 后会自动部署正式环境；也可以从 main 手动运行。请按 [Azure 分步说明](azure-dotnet-10-deployment.zh-CN.md) 操作。

部署机需要相应 .NET 10 ASP.NET Core Runtime；IIS 需要兼容的 Hosting Bundle。发布包应在目标环境用正确的连接字符串、JWT 配置和前端 API 地址验证后再替换旧版。

本次没有修改现有数据库，因此回退本次代码可使用升级前分支或旧发布包，旧版运行仍需要 .NET 8 Runtime。若以后另行执行了数据库结构迁移，回退前需单独评估数据库兼容性，不能只替换 DLL。

.NET 10 LTS 官方支持到 2028-11-14，但仍应定期安装 10.0.x 安全维护更新。

参考：[微软支持政策](https://dotnet.microsoft.com/zh-cn/platform/support/policy)、[ASP.NET Core 10 升级说明](https://learn.microsoft.com/en-us/aspnet/core/migration/90-to-100?view=aspnetcore-10.0)、[EF Core 9 兼容性变更](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes)、[EF Core 10 兼容性变更](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes)。
