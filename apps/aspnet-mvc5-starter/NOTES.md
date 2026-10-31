# Asp.Net MVC 5 Starter Kit: migration notes

Status: verified. Source: https://github.com/akasarto/aspnet-mvc5-starter-template at `885be06e`. Found by searching GitHub on 2026-10-31; the
repo, licence (MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

## Assessment

Round 2 (webForCore 0.2.0), assessment only. MVC 5 starter template with OWIN, SignalR 2 and ASP.NET Identity,
globalized, .NET Framework 4.7.1. The web project references 13 libraries under `sources/` (identity, data, a SQL
Server store, a migrator, and infrastructure for Azure storage, Cloudinary, file system, MailGun, SMTP, dynamic images).

- `scripts/wfc.py assess`: no blockers; notice `smtpclient-config` at
  `sources/Shared.Infrastructure.Smtp/SystemNetSmtpEmailDispatcherService.cs:42` (documented replacement).
- **Go**, with SQL Server; not migrated yet because of its size (14 projects). Next round.

## Migration

(Every change to the app, in order, and why. Keep this in step with `overlay.patch`.)

## Smoke

(What the probes cover, what passed, and each failure with its cause. The run-by-run record is `LOG.md`; the
latest logs are in `logs/`.)

## Open problems

(Each with its classification and a link to its entry in `/FINDINGS.md`.)
