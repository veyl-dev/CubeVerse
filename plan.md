# CubeVerse implementation plan

## Scope and delivery boundary

This repository is being developed as a native Unity/C# Android game, honoring the engine and platform named in the supplied brief. The initial implementation is a playable offline foundation: real 2×2–5×5 cubie/sticker state, legal face/wide/slice/whole-cube moves through a notation parser, a procedural 3D renderer, drag controls, scramble/restart/undo/redo, Classic/Speedcubing/Practice session modes, local solve timing, a local best single and trimmed Ao5, and settings for sound/haptics. It is not represented as the complete commercial specification. Verified online rankings, authentication, multiplayer/friends, cloud save, billing/ads, full tutorial/solver, advanced variants, release assets and a signed Android App Bundle remain later work because service setup, policy review, credentials and device/build verification are not available in this repository environment.

## Implementation structure

- `Assets/Scripts/Engine/`: engine-only integer coordinates, piece/sticker state, move notation, legal layer turns, scramble construction and solved-state detection.
- `Assets/Scripts/Gameplay/`: 3D cube visualization and snapping animation, touch/mouse gesture interpretation, gameplay flow, local timer/statistics and runtime-created responsive interface.
- `Assets/Tests/Editor/`: Unity Test Framework checks for all supported sizes, turn/inverse invariants, notation and scramble round trips.
- `Assets/Editor/`: editor-time creation of the launch scene and Build Settings entry; no hand-authored Unity scene serialization.
- `ProjectSettings/` and `Packages/`: Unity project configuration, application identifier and test framework dependency.

The Unity project uses only built-in Unity components and runtime-generated geometry/UI; no external art, account service or secret is required to launch the core game. The editor setup creates a portrait Android-ready launch scene on first import. Android SDK/NDK, signing, target-SDK verification, Play listing assets and publishing still require a configured Unity installation and publisher account.

## Design direction

Three styles considered (tail probabilities): tactile midnight atelier (0.04), neon orbital lab (0.03), and quiet competition console (0.02). Selected: **tactile midnight atelier**—a precise cube-focused workspace with dark navy surfaces, mint/teal interaction cues, warm amber inspection and clean white type.

- **Design Movement:** restrained industrial instrument design with a soft-lit, physical puzzle centerpiece.
- **Core Principles:** cube-first; controls remain thumb-reachable; state changes are explicit; cosmetics never affect puzzle mechanics.
- **Color Philosophy:** midnight blue gives the cube contrast, teal marks confident actions, amber reserves attention for inspection, and sticker colors remain bright and distinguishable.
- **Layout Paradigm:** portrait instrument panel with a narrow identity/mode rail above the cube and a compact action dock below it.
- **Signature Elements:** a two-letter CV mark with a small teal slash; thin uppercase mode labels; sticker-like color chips and measured numeric timer.
- **Interaction Philosophy:** drags on a face turn the selected layer, while drags outside the cube orbit the view; buttons provide accessible alternatives for session management.
- **Animation:** layer rotations ease smoothly and snap to exact quarter turns; menus update directly without blocking the solve; backgrounding pauses the local solve clock.
- **Typography System:** bundled Unity Arial/legacy system face for broad device fallback; bold numerals for timer, regular text for instructions and compact uppercase for metadata.
- **Brand Essence:** an approachable, precise cube-solving companion for learners and speedcubers; **focused, tactile, fair**.
- **Brand Voice:** concise and calm. Examples: “Scrambled · your solve begins with the first turn.” / “Solved · saved to this device.”
- **Wordmark & Logo:** CV monogram plus a slash suggesting a quarter-turn, paired with a spaced CUBEVERSE wordmark.
- **Signature Brand Color:** mint teal (`#19C79A`), used for primary actions and success feedback.

## Repository outcomes

Implement the playable offline core and regression tests first; keep backend, online competition, monetization and release preparation explicitly documented as incomplete until real integrations are selected and validated. No claim of Play Store readiness, current Google Play compliance, device QA, or global leaderboard verification is made by this source-only implementation.
