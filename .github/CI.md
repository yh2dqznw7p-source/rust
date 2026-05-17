# Continuous Integration

Two workflows live in `.github/workflows/`:

## 1. `validate.yml` — fast checks (always on)

Runs on every push and PR. **No Unity license required**, so it works
on forks immediately. Catches:

- missing project files (asmdefs, docs, manifest, etc.),
- malformed JSON / asmdef,
- editor version drift (must be 2022.3.x LTS),
- accidental HDRP references,
- C# syntax errors via a Roslyn-based parser.

Typical runtime: **30–60 seconds**.

## 2. `unity-build.yml` — full Unity build (gated on license)

Builds the project on Linux runners using
[GameCI](https://game.ci) Docker images. Two matrix legs:

| Job | Platform | Artifact name | What you get |
|---|---|---|---|
| `build` | `StandaloneLinux64` | `rustlike-StandaloneLinux64` | `RustLike.x86_64` + Data folder |
| `build` | `StandaloneWindows64` | `rustlike-StandaloneWindows64` | **`RustLike.exe`** + Data folder + DLLs |

Artifacts are uploaded with **30 day retention**. To download:
**Actions** tab → click the run → scroll to bottom → **Artifacts** section.

The build is driven by `Assets/Editor/Build.cs::Run`. It auto-creates an
empty `Bootstrap.unity` scene if one doesn't exist, since Phase 0 builds
the scene at runtime via `RuntimeInitializeOnLoadMethod`.

### Required secrets

Set them at *Repo → Settings → Secrets and variables → Actions → New repo secret*:

| Secret | Required | Description |
|---|---|---|
| `UNITY_LICENSE` | ✅ | Contents of `Unity_lic.ulf`. |
| `UNITY_EMAIL` | ✅ | Unity account email. |
| `UNITY_PASSWORD` | ✅ | Unity account password. |
| `UNITY_SERIAL` | optional | Unity Pro serial (only if you have Pro). |

Without `UNITY_LICENSE` the `build` jobs are auto-skipped (via the
`precheck` gate), so first-time clones stay green.

### Getting a Personal license file (free)

The cleanest way:

1. **In Unity Hub** (on your local PC):
   - Sign in with your Unity ID.
   - Preferences → Licenses → Add → "Get a free personal license".
2. **Locate the .ulf file** Unity Hub generated:
   - Windows: `%LOCALAPPDATA%\Unity\Licenses\Unity_v2022.x.ulf`
   - macOS:   `~/Library/Application Support/Unity/Unity_v2022.x.ulf`
   - Linux:   `~/.config/Unity/licenses/Unity_v2022.x.ulf`
3. **Open the file in any text editor**. It's an XML blob.
4. **Copy the entire content** (including `<?xml...?>` and trailing newline).
5. Paste it as the `UNITY_LICENSE` repo secret.
6. Push or run the workflow manually (Actions → Unity build → Run workflow).

Full guide with screenshots:
<https://game.ci/docs/github/activation/activation-personal>

### Caching

`unity/Library` is cached by `actions/cache@v4`. After the first cold build
(~10–12 minutes), warm builds finish in **3–5 minutes** per platform.

The cache key includes `manifest.json` hash, so package updates invalidate
it automatically.

### Forks / contributors

Forks won't have access to your secrets. The `validate.yml` workflow still
runs on PRs from forks and gives strong feedback. Maintainers can re-trigger
the Unity build manually after review.

### How to get the .exe (TL;DR)

1. Add 3 secrets above to the repo.
2. Push any commit, or click **Actions → Unity build → Run workflow**.
3. Wait ~12 minutes (first run; ~5 minutes on warm cache).
4. Click into the run → scroll down → **Artifacts** → download
   `rustlike-StandaloneWindows64.zip`.
5. Unzip → run `RustLike.exe`.

### Why no IL2CPP for Windows?

Cross-compiling Windows IL2CPP from a Linux runner is not supported by
Unity. Mono backend produces a real `.exe` and is plenty fast for Phase 0.
If you ever need IL2CPP `.exe` (smaller startup, no Mono runtime),
switch the matrix entry to `runs-on: windows-2022` — costs ~2× CI minutes.
