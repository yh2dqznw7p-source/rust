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
[GameCI](https://game.ci) Docker images. Three matrix legs:

| Job | Platform | Notes |
|---|---|---|
| `build` | StandaloneLinux64 | client |
| `build` | StandaloneWindows64 | client |
| `server-build` | StandaloneLinux64 + `-standaloneBuildSubtarget Server` | headless dedicated server |

Artifacts are uploaded with 14 day retention.

### Required secrets

Set them at *Repo → Settings → Secrets and variables → Actions → New repo secret*:

| Secret | Required | Description |
|---|---|---|
| `UNITY_LICENSE` | ✅ | Contents of `Unity_lic.ulf`. |
| `UNITY_EMAIL` | ✅ | Unity account email. |
| `UNITY_PASSWORD` | ✅ | Unity account password. |
| `UNITY_SERIAL` | optional | Unity Pro serial. |

Without `UNITY_LICENSE` the `build` and `server-build` jobs are auto-skipped
(via the `precheck` gate), so first-time clones stay green.

### Getting a Personal license file

1. Run once locally:
   ```
   docker run --rm -v "$(pwd):/work" -w /work \
     unityci/editor:ubuntu-2022.3.40f1-base-3 \
     unity-editor -batchmode -nographics -logFile /dev/stdout \
       -createManualActivationFile -quit
   ```
2. Upload the produced `Unity_v2022.x.alf` to
   <https://license.unity3d.com/manual> → download `Unity_v2022.x.ulf`.
3. Paste the file's full contents into the `UNITY_LICENSE` secret.

Full guide: <https://game.ci/docs/github/activation/activation-personal>

### Caching

`unity/Library` is cached by `actions/cache@v4`. After the first cold build
(~12 minutes), warm builds finish in **3–5 minutes** per platform.

The cache key includes `manifest.json` hash, so package updates invalidate
it automatically.

### Forks / contributors

Forks won't have access to your secrets. The `validate.yml` workflow still
runs on PRs from forks and gives strong feedback. Maintainers can re-trigger
the Unity build manually after review.
