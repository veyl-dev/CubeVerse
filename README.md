# CUBEVERSE

A native Unity/C# Rubik-style cube game foundation for Android. This repository currently contains a playable offline core, not a completed commercial release.

## Open and play

1. Open the repository in Unity Hub using **Unity 2022.3.62f1** (with Android Build Support if building Android).
2. Let the editor import scripts. `Assets/Editor/CubeVerseProjectSetup.cs` creates `Assets/Scenes/CubeVerse.unity` and adds it to Build Settings automatically; a menu repair command is available at **CubeVerse → Create or Repair Playable Scene**.
3. Press Play. Choose cube size (2×2 through 5×5), choose Classic/Speedcubing/Practice, then press **Scramble**.
4. Drag across a sticker to turn the inferred layer. Drag outside the cube to orbit the view. Undo, redo and restart are in the lower action dock. Speedcubing starts with a 15-second inspection; the first turn starts timing.

The project renders its cube and interface from Unity primitives; no asset download is needed. Local solve records/settings use PlayerPrefs. The scramble is a legal move-sequence scramble generator, not an official WCA random-state generator. Local results are not submitted to or verified by any server.

## Tests

Open **Window → General → Test Runner** in Unity and run the EditMode suite in `Assets/Tests/Editor/CubeModelTests.cs`. Unity Editor is not installed in the authoring sandbox, so source and test files are provided but Unity compilation/tests and Android hardware behavior have not been verified in this environment.

## Build readiness

The editor setup sets the Android application ID to `com.veylstudio.cubeverse`, portrait orientation, and the latest installed Android SDK target. An Android SDK/NDK, a publisher-controlled signing key and a final target API review are required to create an AAB. The project does not currently contain authentication, online leaderboards, cloud saves, friends, anti-cheat, billing, ads, store art, privacy/legal documents or a release build.

See [PROJECT_STATUS.md](PROJECT_STATUS.md) for completed, unverified, incomplete and blocked scope, and [plan.md](plan.md) for the architecture and design direction.
