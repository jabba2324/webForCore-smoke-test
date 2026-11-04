# IdentityServer3 example: migration notes

Status: blocked (webForCore 0.7.0, 7/10 probes on Linux): IdentityServer3 2.5.0 needs .NET Framework's
`System.IdentityModel` (Windows Identity Foundation), which .NET doesn't have ([F69](../../FINDINGS.md#f69)).

scottbrady91/IdentityServer3-Example at `4c810334`, MIT (`LICENSE`). `src/ScottBrady91.IdentityServer3.Example`: an
OpenID Connect provider, IdentityServer3 2.5.0 under `/core` on Katana 3.0.1 with `Microsoft.Owin.Host.SystemWeb`
(`[assembly: OwinStartup]`, chosen by `owin:AppStartup` = `InMemory`), in-memory clients, scopes and users, signed
with a test certificate embedded in the assembly (`Configuration/idsrv3test.pfx`). Two other startups (ASP.NET
Identity 2 on EF, WS-Federation) are compiled but not selected. webForCore 0.7.0 packages (0.8.0 wasn't released when
this was migrated), webForCore-thirdparty 0.6.0 (not used by the app), drawingForCore 0.2.0.

## Assessment

`scripts/wfc.py assess`: no blockers in the app's code. OWIN + Identity (`owin`, `identity`), .NET Framework 4.5.1
(`<httpRuntime targetFramework="4.5.1">`). Packages:

- (a) replaced by webForCore: Microsoft.Owin, Microsoft.Owin.Host.SystemWeb, Owin, Microsoft.AspNet.Identity.Core and
  .EntityFramework 2.2.1;
- (b) kept: IdentityServer3 2.5.0, IdentityServer3.WsFederation 2.5.0, IdentityServer3.AspNetIdentity 2.0.0,
  IdentityModel 1.11.0, EntityFramework 6.1.3 (→ 6.5.1), Newtonsoft.Json 8.0.3 (→ 13.0.4);
- (c) .NET Framework-only binaries to test: IdentityServer3 (`lib/net45` only; it merges its own copies of Autofac and
  Web API into its assembly) and IdentityServer3.WsFederation. IdentityServer3's assembly references
  `System.IdentityModel` 4.0.0.0 (`X509SigningCredentials`, `SecurityTokenHandler`, `X509SecurityToken`,
  `System.IdentityModel.Protocols.WSTrust`), a .NET Framework assembly with no .NET build.

No IIS reference responses; the probes check what an IdentityServer3 deployment answers (discovery, the key set, an
authorization request's redirect to the log-in page).

## Migration

- `scripts/convert.py`: `.csproj` → `.csproj.netfx`; SDK project with `webForCore`,
  `webForCore.Microsoft.Owin.Host.SystemWeb`, `webForCore.Microsoft.AspNet.Identity.EntityFramework` and `.Core` 0.7.0;
  the app's other packages kept (EntityFramework 6.1.3 → 6.5.1, Newtonsoft.Json 8.0.3 → 13.0.4: F7); the embedded
  certificate kept as an `EmbeddedResource` (the `RootNamespace` keeps its manifest name,
  `ScottBrady91.IdentityServer3.Example.Configuration.idsrv3test.pfx`); `packages.config` → `.netfx`; `Program.cs`
  (no session state). The `<Compile>` list equals the glob.
- `app.toml`: `https = true`, as IdentityServer3 is configured with `RequireSsl = true`.
- Nothing else. `web.config` is unchanged.

The build passes.

## Smoke

Round 7 candidates (webForCore 0.7.0; Linux): **7/10**. The seven forbidden-path checks pass. Every request under
`/core` is a 500: the OWIN startup fails where it calls `UseIdentityServer`, before IdentityServer3 runs any of its
code:

```
System.IO.FileNotFoundException: Could not load file or assembly 'System.IdentityModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
   Owin.UseIdentityServerExtension.UseIdentityServer(IAppBuilder app, IdentityServerOptions options)
   ScottBrady91.IdentityServer3.Example.Startup.<>c.<Configuration>b__0_0(IAppBuilder coreApp) in Startup.cs:19
   Owin.MapExtensions.Map(IAppBuilder app, String pathMatch, Action`1 configuration)
```

webForCore's OWIN host and the `OwinStartup` selection worked (the app's `Startup` ran). Getting further needs
IdentityServer3 on a `System.IdentityModel` that .NET has: none exists for WIF (IdentityServer4 and later are
ASP.NET Core only), so this is blocked until there's one (F69).

## Open problems

- [F69](../../FINDINGS.md#f69) (guide gap, raised as [#119](https://github.com/xforcore/webForCore/issues/119)): `System.IdentityModel` (WIF) isn't on .NET, and webForCore's docs don't
  list it; libraries built on it (IdentityServer3, IdentityServer3.WsFederation) can't load.
