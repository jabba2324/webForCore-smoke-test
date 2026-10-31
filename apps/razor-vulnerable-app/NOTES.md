# RazorVulnerableApp: migration notes

Status: rejected. Source: https://github.com/cnotin/RazorVulnerableApp at `780350f7`. Found by searching GitHub on 2026-10-31; the
repo, licence (MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

## Assessment

Rejected after fetching it: `Controllers/HomeController.cs` passes the posted `razorTpl` to `RazorEngine.Razor.Parse`, so
anyone who can reach the app runs arbitrary C# (its README says so: a server-side template injection demo). It is one
controller and one view on MVC 5 plus RazorEngine 3.10, so it adds little that the other MVC apps don't cover, and
running it, even bound to 127.0.0.1, isn't worth it. Not migrated.

## Migration

(Every change to the app, in order, and why. Keep this in step with `overlay.patch`.)

## Smoke

(What the probes cover, what passed, and each failure with its cause. The run-by-run record is `LOG.md`; the
latest logs are in `logs/`.)

## Open problems

(Each with its classification and a link to its entry in `/FINDINGS.md`.)
