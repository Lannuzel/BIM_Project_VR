# BIM Project VR

*Version française : [README.fr.md](README.fr.md).*

<img src="docs/images/avatar.png" alt="Network avatar of a participant (head, body, role label and controllers)" width="240" align="right">

Virtual-reality research application (CESI LINEACT): a triad collaborates on a BIM-inspired task
while the application records voice, gaze, facial expressions and positions to study collective
intelligence.

- **Engine**: Unity 2022.3.56f1 (`ProjectSettings/ProjectVersion.txt`), Asset Serialization set to Force Text.
- **Headset**: Meta Quest Pro (eye tracking and face tracking required), Android API 32.
- **Network**: Photon Fusion 2.0.3, Shared mode, Photon cloud, region `eu` (see `docs/reseau.md`).
- **Roles**: Reader (*Lecteur*: instructions, starts and ends the activity), Calculator (*Calculateur*:
  notepad, calculator, documents), Modeller (*Modélisateur*: creates, moves and measures objects),
  Experimenter (*Expérimentateur*: administration panel, outside the task).

Each participant is represented by the avatar shown opposite (`Assets/Prefabs/PlayerPrefabMB.prefab`):
a translucent head and body, two eyes that follow the headset, the role displayed above the head
and the two Touch Pro controllers. The picture is produced by **BIM VR Tools › Capture the avatar**
(`Assets/Editor/AvatarSnapshot.cs`).

## Scenarios

| Scenario | Scene | Task | Status |
| --- | --- | --- | --- |
| Tutorial (shared) | `Assets/EXP1/Scenes/Tutorial/BIM-EXP_Tutorial_V2.unity` | grabbing, teleporting, drawing lines, moving an object, choosing a role | tested |
| XP1 | `Assets/EXP1/Scenes/Exp1_V3.unity` | laying out a classroom (tables and chairs) | tested (acceptance run of 5 November 2026) |
| XP2 | `Assets/EXP2/Scene/Exp2-V1-09-06-2025_EXP2_User.unity` | reservations for ducts and pipes in walls and floors | tested (acceptance run of 5 November 2026) |
| XP3 | `Assets/EXP3/Scenes/Exp3-V0.2-10-02-26.unity` | ceiling equipment (luminaires, air terminals, fans), scaling against a reference slab, air-change rate, reservations | **not yet tested with participants** |

Each experiment is a separate APK: scene 0 = tutorial, scene 1 = experiment
(`SceneLoader` loads index 1 at the end of the tutorial or on "Skip the tutorial").

XP3 comes from the `Network` branch (April 2026, Mukesh Barange), integrated on 6 November 2026.
That branch had been committed without its `.meta` files: they were rebuilt, keeping the original
identifiers wherever the scenes allowed them to be recovered. The scene builds, but no session has
validated the scenario on a headset yet. Known point: the `PDFCanvas` object of the scene carries a
missing script (already the case in XP1 and XP2, with no effect).

## Branches and tags

- `main`: integrated version.
- `fix/recette-2026-11-05`: fixes from the acceptance run of 5 November 2026, one commit per item
  (identifiers `F1`–`F19`, `N1`–`N5` of the requirements document), then XP3 integration and cleanup.
- `feedback`: real-time feedback system (folder `1_Emery`, Emery Descours), based on the `main`
  of December 2024; not integrated.
- Tags: `v3.0-avant-recette` (state of April 2026 before the acceptance run), `archive/BIM_Interaction`,
  `archive/BIM_Clean` (old branches from January 2025).

## Documents

| File | Content |
| --- | --- |
| `docs/cahier_des_charges.md` | requirements F1–F18, N1–N5, defects, points to clarify (French) |
| `docs/recette_2026-11-05.md` | transcript of the test voice memo (French) |
| `docs/reseau.md` | network requirements (Photon ports), pre-session check, behaviour on connection loss (French) |
| `docs/clavier_natif_quest.md` | study of the native Meta keyboard (not enabled) (French) |
| `docs/check_session.py` | integrity check of a session's files (see below) |
| `docs/receive_data.py` | HTTP receiver that collects the session files from the headsets (see below) |
| `docs/check_references.py` | lists (and can restore) assets still referenced by scenes, prefabs or materials but missing from the project; run after any cleanup (see below) |

## Building the APKs

In Unity: menu **Outils BIM VR › Générer l'APK XP1 / XP2 / XP3** (`Assets/Editor/BuildExperiments.cs`).
From the command line, from any folder:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.56f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath "<project folder>" -buildTarget Android `
  -executeMethod BuildExperiments.BuildXP1 -outputDir "<output folder>" -logFile build_xp1.log
```

Same command with `BuildExperiments.BuildXP2`, `BuildXP3` or `BuildAll` (all three). The APKs have
distinct identifiers (`CollectiveIntelligence.CESI.EXP1`, `.EXP2`, `.EXP3`) and coexist on one
headset. The Android SDK must contain platform 32 (`sdkmanager "platforms;android-32"`).
Install with `adb install -r <file.apk>`.

Before each build, `BuildExperiments` rebuilds the Photon Fusion network prefab table from the
`FusionPrefab` labels and refuses a suspicious table: a stale table (prefabs removed or added without
reimporting `NetworkProjectConfig`) silently breaks every object creation on the headset. The menu
**Outils BIM VR › Vérifier la table des prefabs Fusion** lists the table for diagnosis.

The only errors logged during a build come from the Meta package (`AvatarBehaviourFusion`,
exception on a `[Networked]` property): they are known and do not prevent the build.

## Session configuration

All session settings live in a single asset: `Assets/Resources/SessionConfig.asset`
(select the file in Unity, the fields appear in the Inspector; they are also shown in the
experimenter panel).

| Field | Purpose | Current value |
| --- | --- | --- |
| `sessionName` | name of the Photon session joined by the headsets | `Modelisateur` |
| `photonRegion` | fixed Photon region | `eu` |
| `maxPlayers` | players expected before the activity can be started | 1 (tests); set 3 for a session |
| `sessionTimeoutMinutes` | automatic stop of the recordings | 30 |
| `syncLeadSeconds` | lead given to the network start time | 1 |
| `exitDelayAfterStop` | delay before closing after the thank-you window | 6 s |
| `audioFromSceneStart` | voice recording starts when the scene loads, like the other trackers (the sync beep is inserted at activity start) | true |
| `playSyncBeep` | the sync beep is also played in the headset at activity start | true |
| `startInFrontOfPanel` | at scene load, the headset is recentred in front of the welcome panel (tutorial) or the role panel (experiments), whatever its physical orientation | true |
| `startDistance` | distance between headset and panel at start | 1.6 m |
| `uploadUrl` | data receiver on the PC (empty = no upload) | empty |
| `uploadTimeoutSeconds` | maximum time allowed for the upload | 180 |

The common base of the experiment scenes (movable windows, keyboard, undo stack, Experimenter role,
multi-line notepad) is installed by code when the scene loads (`Assets/Scripts/Core/SessionCore.cs`):
nothing to copy from one scene to another.

## Running a session

1. The three headsets on the same network (see `docs/reseau.md`). To collect the data
   automatically, start the receiver on the experimenter's PC and put its URL in `SessionConfig`
   before building the APKs:

   ```powershell
   python docs/receive_data.py --port 8765 --out .\sessions
   ```

2. Tutorial on each headset; the role can be chosen from the tutorial onwards. On every scene load the
   headset is recentred in front of the starting panel (`startInFrontOfPanel`).
3. Experiment scene: the trackers, voice included, record from the scene load. Pick the role in the list, then "Rejoindre" (join). The counter
   "Joueurs : n/N" shows the connection state (N = `maxPlayers` from `SessionConfig`).
4. The Reader presses "Lancer l'activité" (start the activity): a waiting window appears on each
   headset, then the `MARKER` line is written at the same network time on the three headsets, the
   sync beep is inserted in the audio file and played in the headset, and the session manifest is
   written.
5. The Reader ends the activity, or the automatic stop occurs after `sessionTimeoutMinutes`:
   thank-you message in a panel that follows the head, upload of the files to the PC if a URL is
   configured (status shown under the message), then the application closes on each headset. The files stay on the headset in
   every case.

### Controls

- **Menu**: X button (left controller). **Delete**: Y button. **Undo the last action**: button in
  the Object editing, Measure and Reservation menus, or hold Y for 0.6 s.
- **Move / rotate an object**: right grip trigger to grab, A button to switch mode; arrows (move)
  or rings (rotate) are drawn on the object, in the tutorial as in the experiments.
- **Windows** (documents, notepad, calculator, keyboards): point and hold the right grip trigger
  to move them; red cross at the top right to close them (for the calculator, a small square at the
  top-right corner of its body, since its canvas is not in the plane of its face).
- **Keyboard**: AZERTY, Enter validates the field (line break only in the notepad), "Fermer"
  (close) key.
- **Experimenter**: panel with recording start / stop, forced end of session (with confirmation),
  live role change, simulated players, real-time monitoring of the recordings.

## Recorded data

On the headset: `Android/data/<application identifier>/files/Data/`.

| File | Content |
| --- | --- |
| `*_Session.json` | manifest: session, role, scene, headset, application version, marker and end times (local, network, UTC), files produced and their size |
| `*_Audio.wav` | voice, 16-bit PCM mono 44.1 kHz, starts when the scene loads (`audioFromSceneStart`), synchronisation beep inserted when the activity starts (1 kHz, 0.5 s, `Assets/Resources/SyncSignal.wav`) |
| `*_EyeTrackingData.csv` | gaze: origin, hit point, object looked at, at frame rate |
| `*_FaceTrackingData.csv` | 71 facial action units, at frame rate |
| `*_UsersPositions.csv` | positions, 2 Hz |
| `*_ChairePositionData.csv` / `*_ReservationPositionData.csv` / `*_CalepinagePositionData.csv` | end-of-session summary: objects, reservations or equipment from all headsets |

The gaze, face and position CSV files start with three time columns: `Time` (seconds since the
application was launched on this headset), `NetTime` (Photon Fusion network clock, identical on
the three headsets of a session, -1 outside a session) and `Utc` (headset UTC time, ISO 8601).
Headsets are aligned on `NetTime`. Each CSV contains a `MARKER` line at start and ends with `END`.
Check with:

```powershell
python docs/check_session.py <Data folder copied from the headset>
```

## Architecture (key points)

- `Assets/Scripts/Core/`: `SessionConfig` (configuration asset), `SessionCore` (base installed by
  code in the experiment scenes), `StartPlacement` (headset recentred in front of the starting panel),
  `SessionClock` (time columns of the CSV files),
  `SessionManifest` (JSON manifest), `DataUploader` (HTTP upload of the files at the end of a session).
- `Assets/Scripts/Network/NetworkManager.cs`: Fusion session, automatic reconnection after a
  connection loss, `GetLocalPlayerName()` for the local role.
- `Assets/Scripts/SyncManager.cs`, `HostController.cs`, `ExitManager.cs` (on the avatar
  `PlayerPrefabMB`): synchronised start and stop, end of session.
- `Assets/Scripts/Network/AdminTools.cs`, `Assets/Scripts/UI/ExperimenterPanel.cs`: experimenter tools.
- `Assets/Scripts/Undo/UndoManager.cs`: Modeller undo stack (networked Despawn / Spawn /
  teleport; XP3 scaling cannot be undone).
- `Assets/Scripts/Tutorial/TutorialManager.cs`: progression and validation of the tutorial steps;
  `ModeGizmo.cs`: move / rotate gizmo on the tutorial object (same drawing as
  `TranslateGizmoDrawer` / `RotationGizmoDrawer` in XP2).
- `Assets/BIM Resources/Scripts/BIMBarange/`: Modeller tools (selection, measurement, alignment,
  distance to wall, reservations). `ObjectInteractionHandler.HeightFor` gives the placement height
  of an object: 0 (floor) in XP1, ceiling height by object name in XP3 (`spawnHeightRules`).
- Interfaces created in code: use `XRUiCanvas.MakeInteractive` so that they respond to the Meta
  laser pointer (`OVRRaycaster`).

## Repository content

The repository only contains what the four build scenes reach, plus the folders loaded by name
(`Resources`, `StreamingAssets`, `Editor`, `Scripts`, SDKs). The cleanup of 6 November 2026
removed the kits unrelated to the experiment (platformer, VRKeys, ShadersCollection, XRI
multiplayer template and its editor tutorials, Netcode for GameObjects and the Unity multiplayer
services, PDF libraries for iOS / macOS / UWP / WebGL), the old test scenes and the scripts that
no scene, prefab or other script references. Everything remains in the git history.

Rules to keep the repository healthy:

- never ignore `*.meta`: every asset must be versioned with its `.meta`;
- build archives, logs and IDE settings are excluded by `.gitignore`;
- a test scene does not go into `Assets/EXP*/Scenes`; the Build Settings list only the tutorial
  and the three experiment scenes;
- never delete a folder wholesale without checking the references into it: the cleanup of
  6 November removed materials of the lines and of the tutorial cube, the room model and the Fusion
  prefabs' dependencies that way (magenta objects on the headset, object creation broken). Run
  `python docs/check_references.py .` after any deletion: it must report no `MANQUANT` line;
- shader include files (`*.cginc`, `*.hlsl`) are referenced by name, not by GUID: a dependency
  analysis does not see them. The two includes used by the avatar and keyboard shaders
  (`TexturedStableFresnelCommon.cginc`, `MixedRealityShaderUtils.cginc`) were restored on
  6 November 2026 after the cleanup; a magenta avatar means an include is missing.

## Citing the application

Source code: <https://github.com/Lannuzel/BIM_Project_VR>.

If you use this application in research work, please cite:

> Tristan Lannuzel, Beatrice Biancardi, Mukesh Barange, Emery Descours, Stéphanie Buisine.
> *BIM Project VR: a multi-user virtual reality application for the study of collective
> intelligence.* CESI LINEACT. <https://github.com/Lannuzel/BIM_Project_VR>

The feedback part (`feedback` branch), developed by Emery Descours, was the subject of a
publication:

> Emery Descours, Tristan Lannuzel, Beatrice Biancardi, Mukesh Barange, Stéphanie Buisine.
> Étude de systèmes de feedback en réalité virtuelle pour l'amélioration de l'intelligence
> collective. *IHM'26 - 37e Conférence Internationale Francophone sur l'Interaction Humain-Machine*,
> AFIHM; Université de Lille, October 2026, Villeneuve-d'Ascq, France. ⟨hal-05781898⟩ <https://hal.science/hal-05781898/>
