// Cross-platform CI startup check. No Azure credentials or database access required.
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { readFile } from 'node:fs/promises';
import net from 'node:net';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';

const publishDirectory = path.resolve(process.argv[2] ?? 'artifacts/publish');
const runtime = JSON.parse(await readFile(path.join(publishDirectory, 'OtakuQuest.Server.runtimeconfig.json'), 'utf8'));
assert.equal(runtime.runtimeOptions.tfm, 'net10.0', 'Publish output must target .NET 10');
await readFile(path.join(publishDirectory, 'wwwroot/index.html'));

const listener = net.createServer();
listener.listen(0, '127.0.0.1');
await once(listener, 'listening');
const port = listener.address().port;
await new Promise((resolve, reject) => listener.close(error => error ? reject(error) : resolve()));
const base = `http://127.0.0.1:${port}`;
const server = spawn('dotnet', [path.join(publishDirectory, 'OtakuQuest.Server.dll')], {
    cwd: publishDirectory,
    windowsHide: true,
    env: {
        ...process.env,
        ASPNETCORE_URLS: base,
        ASPNETCORE_ENVIRONMENT: 'Production',
        DOTNET_ENVIRONMENT: 'Production',
        ASPNETCORE_HOSTINGSTARTUPASSEMBLIES: '',
        ConnectionStrings__DefaultConnection: 'Server=127.0.0.1,1;Database=UnusedCiDatabase;User Id=unused;Password=UnusedCiOnly;Connect Timeout=1',
        Jwt__Key: 'CI-Startup-Only-Not-For-Production-Long-Key-2026',
        Jwt__Issuer: 'CiStartup',
        Jwt__Audience: 'CiStartup',
        Logging__LogLevel__Default: 'Error',
    },
    stdio: ['ignore', 'pipe', 'pipe'],
});
let output = '';
let spawnError;
server.on('error', error => { spawnError = error; });
server.stdout.on('data', chunk => { output = (output + chunk).slice(-12000); });
server.stderr.on('data', chunk => { output = (output + chunk).slice(-12000); });
try {
    let ready = false;
    for (let attempt = 0; attempt < 60; attempt++) {
        if (spawnError) throw spawnError;
        if (server.exitCode !== null) throw new Error('Published server exited before startup');
        try {
            const response = await fetch(`${base}/swagger/v1/swagger.json`, { signal: AbortSignal.timeout(2000) });
            if (response.ok) { ready = true; break; }
        } catch { /* Wait for the local server to listen. */ }
        await delay(500);
    }
    assert.ok(ready, 'Published server must start and serve Swagger');
    const swagger = await (await fetch(`${base}/swagger/v1/swagger.json`)).json();
    assert.ok(swagger.paths['/api/Auth/login'], 'Swagger must contain the login endpoint');
    const homepage = await fetch(base);
    assert.equal(homepage.status, 200);
    const html = await homepage.text();
    assert.ok(html.includes('id="root"'), 'React root must be present');
    const script = html.match(/src="([^"]+\.js)"/);
    assert.ok(script, 'React JavaScript bundle must be referenced');
    const asset = await fetch(new URL(script[1], base));
    assert.equal(asset.status, 200);
    assert.match(asset.headers.get('content-type') ?? '', /javascript/);
    const unauthorized = await fetch(`${base}/api/Todo`);
    assert.equal(unauthorized.status, 401, 'Protected endpoint must reject anonymous requests');
    const invalidToken = await fetch(`${base}/api/Todo`, { headers: { Authorization: 'Bearer invalid-token' } });
    assert.equal(invalidToken.status, 401, 'Protected endpoint must reject invalid JWT');
    console.log('PASS: .NET 10 Production startup, Swagger, React index/bundle, anonymous and invalid JWT rejection. No database queried.');
} catch (error) {
    console.error(output);
    throw error;
} finally {
    if (server.pid && server.exitCode === null && server.signalCode === null) {
        const closed = once(server, 'close');
        server.kill();
        await closed;
    }
}
