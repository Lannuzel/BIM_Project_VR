# Project history

*Version française : [historique.md](historique.md).* This document gathers what belongs to the history of the
repository (branches, integrations, cleanups, acceptance runs); the [README](../README.md) only describes the
application as it is.

## Branches and tags

- `main`: integrated version.
- `fix/recette-2026-11-05`: fixes from the acceptance runs of November 2026, one commit per item
  (identifiers `F1`–`F19`, `N1`–`N5` of the requirements document for the first wave), then XP3 integration,
  repository cleanup, feedback port and feedback from the test sessions.
- Tags:
  - `v3.0-avant-recette`: state of April 2026 before the acceptance run;
  - `archive/feedback`: real-time feedback system of Emery Descours (folder `1_Emery`, based on the `main` of
    December 2024), ported by code into `Assets/Scripts/Feedback/` on 6 November 2026;
  - `archive/BIM_Interaction`, `archive/BIM_Clean`: old branches from January 2025.

## XP3 integration (6 November 2026)

XP3 comes from the `Network` branch (April 2026, Mukesh Barange). That branch had been committed without its
`.meta` files: they were rebuilt, keeping the original identifiers wherever the scenes allowed them to be
recovered. Two materials (light on, fan metal) had never been versioned: they were recreated under their
original GUIDs. The scene builds and was tested alone on a headset, but no three-participant session has
validated the scenario yet. Known point: the `PDFCanvas` object of the scene carries a missing script (already
the case in XP1 and XP2, with no effect).

## Repository cleanup (6 November 2026)

The repository now only contains what the four build scenes reach, plus the folders loaded by name
(`Resources`, `StreamingAssets`, `Editor`, `Scripts`, SDKs). Removed: the kits unrelated to the experiment
(platformer, VRKeys, ShadersCollection, XRI multiplayer template and its editor tutorials, Netcode for
GameObjects and the Unity multiplayer services, PDF libraries for iOS / macOS / UWP / WebGL), the old test
scenes and the scripts that no scene, prefab or other script references. Everything remains in the git history.

Lessons learned (now rules in the README): the cleanup had also removed the materials of the lines and of the
tutorial cube, the room model and dependencies of the Fusion prefabs (magenta objects on the headset, object
creation broken); `docs/check_references.py` was written to find them. The two shader includes of the avatar
and keyboard (`TexturedStableFresnelCommon.cginc`, `MixedRealityShaderUtils.cginc`), referenced by name and not
by GUID, were restored the same day.

## Acceptance runs

- **5 November 2026**: first run on headsets (voice memo transcribed in `recette_2026-11-05.md`, requirements
  in `cahier_des_charges.md`): movable windows, AZERTY keyboard, undo stack, single `SessionConfig`
  configuration, common time columns, session manifest, data upload, experimenter tools, guided tutorial.
- **6 November 2026**: local clock on every headset, configurable start and stop roles, tutorial panel on the
  wall, gizmo cleanup after undo, chair and layout logs, audio gap padding, XP3 materials and icons, mesh
  combining of the BIM models at build time, port of Emery Descours' feedbacks.
- **9 November 2026**: tutorial start position moved back, progress panel moved off the move-object
  instructions, XP3 clock placed on the east wall, role choice removed from the tutorial and hidden
  Experimenter role (A, A, B, B), feedbacks switchable from the panel even when disabled in the
  configuration, README illustration and trimming.

## Known build errors

The only errors logged during a build come from the Meta package (`AvatarBehaviourFusion`, exception on a
`[Networked]` property): they do not prevent the build. At the end of `BuildAll`, the message "Timeout after
300 seconds while waiting async operations" appears after the three APKs have been produced.
