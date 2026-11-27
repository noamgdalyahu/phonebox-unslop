# PhoneBox Unslop

A restyled fork (MIT) of PhoneBox: turns an Android phone into a desk panel / control deck for a computer.
Fork repo: `noamgdalyahu/phonebox-unslop`. Default branch `main`. Work on a feature branch, then fast-forward merge into `main`.
The credit to the original author and the LICENSE stay.

## Layout

- `agent/PhoneBox.Agent/` – the computer-side agent (C#, .NET 10, Windows/macOS/Linux). Settings page: `settings.html`. Windows settings open in a WebView2 window (`AppWindow.cs`).
- `agent/PhoneBox.Agent.Tests/` – agent tests (`dotnet test`).
- `android/` – the phone app. The panel UI is web assets in `android/app/src/main/assets/www/` (`style.css`, `controls.css`, `appearance.js`, `index.html`, `locale-*.js`, `widgets/`). Radii and most styling are hardcoded px values in those files. Native bridge code is in `android/app/src/main/java/net/oxog/phonebox/deskpanel/`.
- `ios/` – iOS app. `tools/` – scripts, release checks, UI tests (`tools/ui-tests`, Playwright). `docs/` – guides (`RELEASING.md`, `APPEARANCE.md`). `scripts/` – install/build/release helpers. `packaging/`, `bucket/` – installers, Scoop.

## Design

The "hairline" theme: near-monochrome, black/white dual theme, 1px hairline borders, small radii, one indigo accent, sentence-case labels, no gradient washes or glows, font weight capped at 500. Keep new UI in that style (the `hairline-ui` skill describes it).

## Commands

- Release metadata check: `python tools/check-release.py` (must pass before any release).
- Agent tests: `dotnet test agent/PhoneBox.Agent.Tests`.
- Android: `./gradlew lintDebug assembleDebug` in `android/`.
- UI tests: `npm test` in `tools/ui-tests`.
- When a user-facing label changes, update the tests and locale files (`python tools/sync-locales.py`).

## Releasing

See `docs/RELEASING.md`. In short:

1. Bump the version in `agent/PhoneBox.Agent/PhoneBox.Agent.csproj` (`<Version>`) and `android/app/build.gradle.kts` (`versionName`, and `versionCode` +1).
2. Move the `CHANGELOG.md` "Unreleased" notes under a dated `## X.Y.Z` heading.
3. `python tools/check-release.py`, commit, merge to `main`, push.
4. `gh workflow run release.yml --repo noamgdalyahu/phonebox-unslop --ref main -f draft=true` (HEAD must equal origin/main).
5. When the run is green, publish the draft (`gh release edit vX.Y.Z --draft=false`). The app only sees published releases.

Releasing is outward-facing: only do it when the user clearly asks, and dispatch it as a draft first.

## Conventions

- Add a `CHANGELOG.md` line under "Unreleased" for every user-visible change.
- The app's update repository, download links and pairing page point to this fork, never to the original project.
- Don't commit secrets or signing keys (they live in GitHub repository secrets).
