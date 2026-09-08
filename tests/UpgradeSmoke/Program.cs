using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;

// Uses only a uniquely named disposable LocalDB database; never reads appsettings.
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var published = args.Length == 1;
var serverDirectory = published ? Path.GetFullPath(args[0]) : Path.Combine(root, "OtakuQuest/OtakuQuest.Server");
var serverDll = Path.Combine(serverDirectory, published ? "OtakuQuest.Server.dll" : "bin/Release/net10.0/OtakuQuest.Server.dll");
var databaseName = "OtakuQuest_UpgradeSmoke_" + Guid.NewGuid().ToString("N");
var connection = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
await using var db = new OtakuQuestDbContext(new DbContextOptionsBuilder<OtakuQuestDbContext>()
    .UseSqlServer(connection).Options);
Process? server = null;
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
try
{
    Check(!db.Database.HasPendingModelChanges(), "EF model matches existing migration snapshot");
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "All existing migrations applied to isolated SQL Server database");
    await db.Database.MigrateAsync();
    Check(true, "Reapplying migrations is a no-op");

    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    var start = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = serverDirectory, UseShellExecute = false, CreateNoWindow = true
    };
    start.ArgumentList.Add(serverDll);
    start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "";
    start.Environment["ConnectionStrings__DefaultConnection"] = connection;
    start.Environment["Jwt__Key"] = "UpgradeSmokeOnly-Key-NotForProduction-2026-LongEnough";
    start.Environment["Jwt__Issuer"] = "UpgradeSmoke";
    start.Environment["Jwt__Audience"] = "UpgradeSmoke";
    start.Environment["Logging__LogLevel__Default"] = "Error";
    server = Process.Start(start) ?? throw new Exception("Could not start server");
    using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(15) };
    var ready = false;
    for (var attempt = 0; attempt < 60; attempt++)
    {
        if (server.HasExited) throw new Exception("Server exited before startup");
        try { using var response = await client.GetAsync("/swagger/v1/swagger.json"); ready = response.IsSuccessStatusCode; }
        catch (HttpRequestException) { }
        if (ready) break;
        await Task.Delay(500);
    }
    Check(ready, "Real Kestrel server starts and generates Swagger");
    if (published)
    {
        var html = await client.GetStringAsync("/");
        Check(html.Contains("id=\"root\""), "Published React index is served");
        var asset = System.Text.RegularExpressions.Regex.Match(html, "src=\"([^\"]+\\.js)\"").Groups[1].Value;
        Check(!string.IsNullOrWhiteSpace(asset), "Published index references JavaScript bundle");
        using var assetResponse = await client.GetAsync(asset);
        Check(assetResponse.IsSuccessStatusCode && assetResponse.Content.Headers.ContentType?.MediaType?.Contains("javascript") == true,
            "Published JavaScript bundle is served with correct content type");
    }
    async Task<JsonNode?> Request(HttpMethod method, string path, object? body = null, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Check(response.StatusCode == expected, $"{method} {path} -> {(int)expected}");
        if (string.IsNullOrWhiteSpace(content) || !response.IsSuccessStatusCode) return null;
        return JsonNode.Parse(content);
    }
    using (var swaggerUi = await client.GetAsync("/swagger/index.html"))
        Check(swaggerUi.IsSuccessStatusCode, "Swagger UI loads");
    await Request(HttpMethod.Get, "/api/Todo", expected: HttpStatusCode.Unauthorized);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
    await Request(HttpMethod.Get, "/api/Todo", expected: HttpStatusCode.Unauthorized);
    client.DefaultRequestHeaders.Authorization = null;
    await Request(HttpMethod.Post, "/api/Auth/login", new { }, HttpStatusCode.BadRequest);
    var credentials = new { username = "upgrade-smoke", password = "Smoke-Test-Password-123!" };
    await Request(HttpMethod.Post, "/api/Auth/register", credentials);
    await Request(HttpMethod.Post, "/api/Auth/register", credentials, HttpStatusCode.BadRequest);
    await Request(HttpMethod.Post, "/api/Auth/login", new { username = credentials.username, password = "wrong" }, HttpStatusCode.BadRequest);
    var login = await Request(HttpMethod.Post, "/api/Auth/login", credentials);
    Check(!string.IsNullOrEmpty(login?["token"]?.GetValue<string>()), "Login returns JWT");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!["token"]!.GetValue<string>());
    var stats = await Request(HttpMethod.Get, "/api/PlayerProfile/my-stats");
    Check(stats!["username"]!.GetValue<string>() == credentials.username, "JWT resolves correct player");
    var initialCurrency = stats["currency"]!.GetValue<int>();
    var task = await Request(HttpMethod.Post, "/api/Todo", new { title = "Upgrade test", description = "Isolated test", type = 0, difficultyRank = 0 });
    var taskId = task!["id"]!.GetValue<int>();
    var tasks = await Request(HttpMethod.Get, "/api/Todo");
    Check(tasks!.AsArray().Any(t => t!["id"]!.GetValue<int>() == taskId), "Created task persists");
    var completed = await Request(HttpMethod.Post, $"/api/Todo/{taskId}/complete");
    stats = await Request(HttpMethod.Get, "/api/PlayerProfile/my-stats");
    Check(stats!["currency"]!.GetValue<int>() == initialCurrency + completed!["currencyReward"]!.GetValue<int>(), "Task reward persists");
    await Request(HttpMethod.Post, $"/api/Todo/{taskId}/complete", expected: HttpStatusCode.BadRequest);
    var item = await Request(HttpMethod.Post, "/api/Item/create", new { name = "Smoke Sword", description = "Test", type = 0, price = 0, isPurchasable = true, imageAsset = "Diamond_Sword", strBonus = 1 });
    var itemId = item!["item"]!["id"]!.GetValue<int>();
    var shop = await Request(HttpMethod.Get, "/api/Item/shop");
    Check(shop!.AsArray().Any(i => i!["id"]!.GetValue<int>() == itemId), "Shop contains created item");
    await Request(HttpMethod.Post, "/api/Item/buy", new { itemId });
    await Request(HttpMethod.Post, "/api/Item/equip", new { itemId });
    var inventory = await Request(HttpMethod.Get, "/api/Item/inventory");
    Check(inventory!.AsArray().Any(i => i!["id"]!.GetValue<int>() == itemId), "Purchased item persists in inventory");
    stats = await Request(HttpMethod.Get, "/api/PlayerProfile/my-stats");
    Check(stats!["weaponName"]!.GetValue<string>() == "Smoke Sword", "Equipped weapon persists");
    await Request(HttpMethod.Post, "/api/Boss/create", new { order = 0, name = "Smoke Boss", description = "Test", imageAsset = "Bowser", maxHp = 1, str = 1, @int = 1, def = 0, rewardXP = 1, rewardCurrency = 1 });
    await Request(HttpMethod.Get, "/api/Boss/current");
    var combat = await Request(HttpMethod.Post, "/api/Boss/attack");
    Check(combat!["bossDefeated"]!.GetValue<bool>(), "Boss combat succeeds");
    Check(await db.Users.CountAsync() == 1 && await db.Tasks.CountAsync() == 1, "SQL Server contains expected test data");
    Console.WriteLine($"SUCCESS: {passed} checks passed on .NET {Environment.Version}");
}
finally
{
    if (server is { HasExited: false }) { server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); }
    server?.Dispose();
    // db uses a fresh GUID name constructed above, never a user-supplied connection.
    await db.Database.EnsureDeletedAsync();
    Console.WriteLine("Cleaned up disposable database: " + databaseName);
}
