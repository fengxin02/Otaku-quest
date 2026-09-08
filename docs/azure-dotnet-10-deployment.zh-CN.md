# Azure 上线 .NET 10：操作顺序

本说明针对仓库的 Azure App Service `otakuquest` 工作流。尚未访问实际 Azure 配置或运行 GitHub Actions。

## 已修改

- 构建 SDK 改为 `10.0.x`，显式安装 Node 22，执行 `npm ci`。
- PR 执行构建、发布包启动检查，不执行 Azure 部署。
- main 分支收到 push 后，在构建检查成功后自动部署 Production；也可以从 main 手动运行工作流。
- 原 Azure 身份 Secrets 引用、应用名、Production 目标保持不变。
- 发布为依赖 .NET 10 运行时的 DLL，`UseAppHost=false` 避免 Ubuntu 构建产生的原生启动器与 Windows 部署目标不匹配。
- 新增 `tests/check-published-app.mjs`，用 Node 标准库在 Production 模式启动发布包，检查 Swagger、首页、JS、匿名与无效 JWT 的 401。使用无效测试连接字符串，不查询数据库，不访问 Azure。
- 前端 API 地址未设置时使用同域 `/api`，避免生成 `undefined/api/...`。

“合并不部署”仅适用于本工作流。若 Vercel 或其他平台也监听 main，应单独确认其自动发布行为。

## 1. 记录现状，先不更改 Azure 运行时

1. Azure Portal → **App Services / 应用服务** → **otakuquest**。
2. 在 **Overview / 概述** 或 **Properties / 属性** 记录 Windows/Linux、资源组、默认域名、App Service Plan 套餐。
3. **Settings → Configuration → General settings / Stack settings** 中记录当前 .NET 版本。门户菜单名称可能因版本不同而变化。
4. 查看 **Deployment Center / 部署中心**，确认使用本仓库 GitHub Actions，没有其他独立自动部署来源。
5. GitHub **Actions** 中记录最近一次升级前成功部署的运行链接和 commit SHA；如果 `.net-app` 附件仍在，下载保存旧发布包。

不要提前把正式 Linux 运行时从 8 改成 10：保存可能重启服务，旧 net8.0 程序未必能在新环境运行。需要先完成构建验证，再安排切换时间。

## 2. 核对前端 API 地址

如果前后端都由同一个 Azure App Service 提供，不需要设置额外变量，默认使用同域 `/api`。

若这个工作流构建出的前端需要访问另一个域名：

1. GitHub 仓库 → **Settings → Secrets and variables → Actions → Variables**。
2. **New repository variable**，Name 填 `VITE_API_BASE_URL`。
3. Value 填实际后端 HTTPS 根地址，从 Azure Overview 复制对应域名。不要填 localhost，不要带 `/api`（生成客户端已经包含 `/api`）。
4. 保存。此值是公开 API 地址，不是数据库密码。

若 React 在 Vercel/其他平台，核对该平台自身的 `VITE_API_BASE_URL`。GitHub 的变量不会同步到 Vercel；后端域名没变时一般不必更换地址。注意那个平台是否会在 merge 后自动发布前端。

Vite 在构建时写入变量，仅在 Azure 修改同名运行时变量不会更新已经生成的 JS，必须重新构建。

## 3. 提交分支并建立 PR

1. 提交这次升级和部署相关文件，Push `feature/Dot_Net_Update`。
2. .csproj、global.json、工作流、main.tsx、测试源码与说明文档应一起提交；bin/obj 不提交。
3. 建立目标为 main 的 Pull Request。
4. 在 **Checks** 查看本工作流的 **build**：SDK、Node、npm ci、编译、publish、Check published app 应全部成功。
5. **PR 中 deploy 显示 Skipped 是正常的**，PR 不发布。
6. 检查通过后 merge；main 会重新构建，build 成功后自动部署 Production。

不要通过旧成功运行记录的 Re-run 来测试新流程，旧记录使用旧提交的工作流。

## 4. 核对 Azure 应用设置和数据库

进入 otakuquest → **Settings → Environment variables / 环境变量**（旧门户可能在 Configuration）：

- 现有连接字符串 `DefaultConnection`，或应用设置 `ConnectionStrings__DefaultConnection`，应仍指向原 Azure 数据库。
- 保留现有 JWT 配置：`Jwt__Key`、`Jwt__Issuer`、`Jwt__Audience` 或当前等效配置，不必重新生成密钥。
- `ASPNETCORE_ENVIRONMENT` 如已设置应为 Production；不要复制本机 SpaProxy 开发配置。
- 不要把测试库连接或测试密钥填进生产环境。

这些值只在 Azure 内核对，不要贴进仓库。原来能访问数据库时，本次升级通常不要求更改 SQL 防火墙或密码。没有新增数据库结构迁移，不需要 Update-Database，更不需要删库重建。不要在 Azure 运行 LocalDB UpgradeSmoke 测试。

## 5. 准备 .NET 10 运行环境

### Windows

1. **Development Tools → Advanced Tools → Go**，打开 Kudu。
2. 在 Debug console 的 CMD/PowerShell 执行 `dotnet --list-runtimes`。
3. 确认出现 `Microsoft.NETCore.App 10.0.x` 和 `Microsoft.AspNetCore.App 10.0.x`。
4. 门户如提供堆栈版本选择，选择稳定 .NET 10。保存可能重启，安排在切换时间内。
5. 托管 App Service 不需要按普通 Windows 虚拟机的方式手动安装 Hosting Bundle。

### Linux

1. 在 Configuration → Stack settings 确认有稳定 .NET 10 可选。
2. 没有选项时先不要部署；GitHub 装 SDK 不会给 Azure 应用安装运行时。
3. 准备正式切换时选择 .NET 10 并保存。旧程序可能在新包发布完前不可用，所以需要维护窗口。
4. 保留原有正确启动命令；若写死旧 net8.0 路径，改为发布目录中的 `dotnet OtakuQuest.Server.dll`，不要使用本机 bin/Release 路径。
5. 如能打开应用容器 SSH，可用 `dotnet --list-runtimes` 核对；独立 SCM 容器的运行时不能代替应用容器的结果。

### 不能接受短暂停机时

先用测试 App Service 或 Deployment Slot 验证。槽位需要 Standard、Premium 或 Isolated 套餐，创建资源可能涉及费用，不要直接升级付费套餐。

当前工作流仍只部署 Production，不会自动创建或部署 staging。采用槽位方案需要先另行配置测试目标部署。测试槽应连接独立测试数据库，复制生产设置可能复制生产库连接；确认运行时和 swap/slot 设置，避免把测试库连接交换进生产。未做这些配置前，不能把直接 Production 发布当作零停机升级。

## 6. 自动发布

完成前面的准备后，将经过检查的 PR merge 到 main。main 的工作流先执行 build；只有 build 成功，deploy 才会自动运行并发布到 Azure Production。普通 PR 不会部署。

需要重新部署 main 当前版本时，可以在 GitHub 仓库 → **Actions** → 选择该工作流 → **Run workflow**，Branch 选择 main。手动运行 main 也会执行部署。

原 Azure 登录 Secrets 名称保持不变；Login to Azure 失败时检查现有 GitHub Secrets、Azure 联合身份和权限，不要改 SQL 密码。

## 7. 验证上线

1. 打开真实前端页面，确认图片和页面正常。
2. 打开真实后端域名的 `/swagger`，确认应用能启动；这不代表数据库已通过测试。
3. 用已有账号登录，查看原任务、背包、资料，确认现有 Azure SQL 数据能读取。
4. 如需测试写入，用专用测试账号创建任务、完成任务并检查奖励。
5. Azure → **Monitoring → Log stream / 日志流** 检查启动和 SQL 异常。

## 8. 出错和回退

| 现象 | 优先检查 |
| --- | --- |
| build 失败 | 对应步骤日志，新包不会进入 deploy |
| deploy 被跳过 | PR 中属于正常；main push 或 main 手动运行时检查事件与分支条件 |
| Login to Azure 失败 | 原 OIDC 身份、Secrets 和授权 |
| 502/503、框架缺失提示或启动失败 | 实际 .NET/ASP.NET Core 10 运行时、启动命令、日志 |
| 首页正常，登录 500 | SQL 连接、网络、JWT 和服务端日志 |
| 前端请求 localhost/undefined/错误域名 | 实际前端平台的 VITE_API_BASE_URL，修正后重建 |

需要回退时重新部署保存的旧发布包，并按需恢复 .NET 8 运行时和原启动设置。利用旧运行记录重建前先确认提交、产物和构建环境仍可用。

回退提交 merge 到 main 后会触发自动部署，但仍需等待 Actions 的 build 和 deploy 完成，并实际检查网站。此次未改现有数据库结构，无需回退数据库；以后另行执行结构迁移则要重新评估。

## 验证范围

本地已检查 YAML 与默认关闭的部署条件、无显式前端 API 地址的构建，以及 Production 发布包启动/页面/JWT 拒绝检查；未访问 Azure SQL。此前隔离 LocalDB 的 39 项业务检查已通过，见升级记录。

GitHub Ubuntu runner 和 Azure 实际环境尚未执行验证，需通过 PR Checks 和上述步骤完成。

参考：[Azure .NET 配置](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore)、[部署槽](https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots)、[手动运行工作流](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)。
