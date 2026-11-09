# BIM Project VR

*Version française : [README.fr.md](README.fr.md).*

![Modeller's view during a session: measurement between two objects, left-hand menu, avatars of the Reader and the Calculator](docs/images/application.jpg)

Virtual-reality research application (CESI LINEACT): a triad collaborates on a BIM-inspired task
while the application records voice, gaze, facial expressions and positions to study collective
intelligence; the indicators are then computed by the
[TRACE](https://github.com/Lannuzel/TRACE---Traceable-Reproducible-Analysis-of-Collective-Emergent-States)
pipeline (see "Recorded data"). Above, two moments of the Modeller's view in XP1, taken from one minute of a session
filmed from the headset.

- **Engine**: Unity 2022.3.56f1 (`ProjectSettings/ProjectVersion.txt`), Asset Serialization set to Force Text.
- **Headset**: Meta Quest Pro (eye tracking and face tracking required), Android API 32.
- **Network**: Photon Fusion 2.0.3, Shared mode, Photon cloud, region `eu` (see `docs/reseau.md`).
- **Roles**: Reader (*Lecteur*: instructions, ends the activity), Calculator (*Calculateur*:
  notepad, calculator, documents), Modeller (*Modélisateur*: creates, moves and measures objects,
  starts the activity), Experimenter (*Expérimentateur*: administration panel, outside the task;
  hidden role, obtained with A, A, B, B on the right controller).

<img src="docs/images/avatar.png" alt="Network avatar of a participant (head, body, role label and controllers)" width="150" align="right">

Each participant is represented by the avatar shown opposite (`Assets/Prefabs/PlayerPrefabMB.prefab`):
a translucent head and body, two eyes that follow the headset, the role displayed above the head
and the two Touch Pro controllers. The picture is produced by **BIM VR Tools › Capture the avatar**
(`Assets/Editor/AvatarSnapshot.cs`).

## Scenarios

| Scenario | Scene | Task | Status |
| --- | --- | --- | --- |
| Tutorial (shared) | `Assets/EXP1/Scenes/Tutorial/BIM-EXP_Tutorial_V2.unity` | grabbing, teleporting, drawing lines, moving an object | tested |
| XP1 | `Assets/EXP1/Scenes/Exp1_V3.unity` | laying out a classroom (tables and chairs) | tested (acceptance runs of November 2026) |
| XP2 | `Assets/EXP2/Scene/Exp2-V1-09-06-2025_EXP2_User.unity` | reservations for ducts and pipes in walls and floors | tested (acceptance runs of November 2026) |
| XP3 | `Assets/EXP3/Scenes/Exp3-V0.2-10-02-26.unity` | ceiling equipment (luminaires, air terminals, fans), scaling against a reference slab, air-change rate, reservations | tested alone on a headset, **no three-participant session yet** |

Each experiment is a separate APK: scene 0 = tutorial, scene 1 = experiment
(`SceneLoader` loads index 1 at the end of the tutorial or on "Skip the tutorial").

The history of the project (branches and tags, XP3 integration, repository cleanup, acceptance runs) is
in `docs/history.md`.

## Documents

| File | Content |
| --- | --- |
| `docs/history.md` | branches, tags, XP3 integration, repository cleanup, acceptance runs, known build errors |
| `docs/cahier_des_charges.md` | requirements F1–F18, N1–N5, defects, points to clarify (French) |
| `docs/recette_2026-11-05.md` | transcript of the test voice memo (French) |
| `docs/reseau.md` | network requirements (Photon ports), pre-session check, behaviour on connection loss (French) |
| `docs/clavier_natif_quest.md` | study of the native Meta keyboard (not enabled) (French) |
| `docs/check_session.py` | integrity check of a session's files (see below) |
| `docs/receive_data.py` | HTTP receiver that collects the session files from the headsets (see below) |
| `docs/check_references.py` | lists (and can restore) assets still referenced by scenes, prefabs or materials but missing from the project; run after any cleanup |
| `docs/images/` | README illustrations (Git LFS) |
| `docs/tutoriel/` | Word sources of the tutorial PDF (`Assets/StreamingAssets/PDFs/Tutorial/`), kept out of the APKs |

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

At build time, `StaticBatchingAtBuild` marks every renderer coming from a fixed FBX model (BIM
models of XP2 and XP3, thousands of geometry nodes) as *Batching Static*, so Unity combines their
meshes in the APK (scenes on disk are untouched). The icons of the XP3 "Générer …" buttons are
rendered from the prefabs by **Outils BIM VR › Générer les icônes des prefabs XP3**
(`Assets/Resources/Icone/XP3_*.png`) and assigned by code at scene load. The known build errors
(Meta package) are listed in `docs/history.md`.

## Session configuration

All session settings live in a single asset: `Assets/Resources/SessionConfig.asset`. Select it
**in Unity**: the fields appear in the Inspector (a field left at its default value is not written to the
`.asset` file, so it does not show up in a text editor). The settings are also shown in the experimenter
panel.

| Field | Purpose | Current value |
| --- | --- | --- |
| `sessionName` | name of the Photon session joined by the headsets | `Modelisateur` |
| `photonRegion` | fixed Photon region | `eu` |
| `maxPlayers` | players expected before the activity can be started | 1 (tests); set 3 for a session |
| `startActivityRole` | role that sees the participant check and the "Lancer l'activité" button | `Modelisateur` |
| `stopActivityRole` | role that sees "Arreter l'enregistrement" once the activity has started | `Lecteur` |
| `sessionTimeoutMinutes` | automatic stop of the recordings | 30 |
| `syncLeadSeconds` | lead given to the network start time | 1 |
| `exitDelayAfterStop` | delay before closing after the thank-you window | 6 s |
| `audioFromSceneStart` | voice recording starts when the scene loads, like the other trackers (the sync beep is inserted at activity start) | true |
| `playSyncBeep` | the sync beep is also played in the headset at activity start | true |
| `startInFrontOfPanel` | at scene load, the headset is recentred in front of the welcome panel (tutorial) or the role panel (experiments), whatever its physical orientation | true |
| `startDistance` | distance between headset and role panel at start | 1.6 m |
| `startDistanceTutorial` | distance between headset and the tutorial welcome panel (4 m wide) | 2.8 m |
| `clockPlacements` | position of the activity clock face per scene (scene-name fragment, position, rotation); without an entry, the prefab pose is kept (whiteboard of XP1 and XP2) | `Exp3` → (6.50; 1.75; 1.20), east wall |
| `feedbackGaze` / `feedbackVoice` / `feedbackFace` | collaborative feedbacks (see below) | false |
| `feedbackScenes` | experiment scenes where the feedbacks apply (empty = all) | empty |
| `uploadUrl` | data receiver on the PC (empty = no upload) | empty |
| `uploadTimeoutSeconds` | maximum time allowed for the upload | 180 |

The common base of the scenes (movable windows, keyboard, undo stack, start recentring, hidden
experimenter mode, feedbacks, multi-line notepad) is installed by code when the scene loads
(`Assets/Scripts/Core/SessionCore.cs`): nothing to copy from one scene to another.

## Experimenter mode

The Experimenter role is hidden: it is offered neither in the tutorial nor in the role list of the
experiment scene (the option is removed at scene load). On the experimenter's headset:

1. press **A, A, B, B** on the right controller, within 3 seconds, in the tutorial or in the experiment
   scene before "Rejoindre";
2. in the experiment scene, the Experimenter role is applied, the "Experimentateur" option appears and is
   selected in the role list, and the experimenter panel opens to the left of the role panel (from the
   tutorial, the role is remembered and all this happens when the experiment scene loads);
3. "Rejoindre" like the other headsets. The experimenter's headset counts as a connected player: set
   `maxPlayers` = 4 when the three participants and the experimenter join the same session.

The panel offers: synchronised recording start / stop, forced end of session (second click to confirm),
Gaze / Voice / Face feedbacks ON / OFF for the three headsets, live role change (back to a participant role
included), simulated players (counted as connected), a reminder of the configuration and real-time
monitoring of the recordings. It moves like the other windows (hold the right grip trigger). The same
A, A, B, B sequence hides or shows the panel again. The role is kept from one launch to the next: to become a
participant again, pick a role in the panel or in the list before "Rejoindre".

## Collaborative feedbacks

Port of Emery Descours' work (branch `feedback`, August 2026, kept as tag `archive/feedback`; see the
IHM'26 article in the citations). Three feedbacks, installed by code in XP1, XP2 and XP3
(`Assets/Scripts/Feedback/`), **off by default**:

| Feedback | Behaviour | Settings |
| --- | --- | --- |
| Gaze (`feedbackGaze`) | when at least two participants look at the same object, it gets a white outline whose opacity rises over 10 s (blue when all three look at it) | — |
| Voice (`feedbackVoice`) | the microphone level of the recording is read (dBFS threshold); the network authority tracks the current speaker and the successful or failed interruptions. Individual: crossed-out microphone for 1 s in the left-hand menu for whoever cut in. Collective: message to all (with a bell) when, over a sliding window, the ratio of successful interruptions over attempts exceeds `voiceInterruptionRatio` and the count exceeds `voiceInterruptionCount` | `voiceDbThreshold` (-70), `voiceTimeRequired` (1 s), `voiceWindowSeconds` (60), `voiceInterruptionRatio` (0.62), `voiceInterruptionCount` (3) |
| Face (`feedbackFace`) | action units 15 (lip corners) and 17 (chin): a light bulb in the left-hand menu whose opacity follows the ratio of samples with expression over the window; a participant is "focused" when the ratio reaches `faceThreshold`; message to all (with a bell) when the three participants are unfocused | `faceWeightThreshold` (0.1), `faceEvaluationSeconds` (60), `faceThreshold` (0.02) |

### Enabling or disabling the feedbacks

Activation is the experimental condition. Two ways, which can be combined:

1. **Before building the APKs**: in Unity, select `Assets/Resources/SessionConfig.asset` and tick
   `feedbackGaze`, `feedbackVoice` and / or `feedbackFace` in the Inspector (`feedbackScenes` optionally
   restricts the effect to some scenes). The three headsets then start with these feedbacks on, and the
   tutorial gains two steps: a sensor check (microphone and face) before the welcome screen, and an
   explanation of the pictograms before launching the experiment.
2. **During the session**: on the experimenter's headset, press A, A, B, B on the right controller to open
   the experimenter panel, then the **Regard** (gaze), **Parole** (voice) and **Visage** (face) buttons
   ("ON" / "OFF" label). The choice is propagated to the three headsets by RPC and takes effect at once,
   before or after the activity starts, even when the configuration left everything off.

The condition is written in the session manifest (`feedbackGaze`, `feedbackVoice`, `feedbackFace`) and every
switch is logged in the feedback log. A network object `FeedbackNetwork` (prefab generated in
`Assets/Resources/Feedback/` by **Outils BIM VR › Créer le prefab réseau des feedbacks**, also at build time)
is spawned by the session master and holds the shared state.

Events are logged in `*_FeedbackData.csv` (same time columns, `MARKER`, `END`, listed in the manifest, uploaded
with the rest): `Feedback` (gaze, voice, face, session), `Event`, `Role`, `Target`, `Value`, `Count`, `Total`,
`Ratio`, `Who`. The file is only created when a feedback was on or switched during the session.

Documented differences from the published version: gaze detection covers every object with a collider (stable
identifier computed by code), not only network objects, which makes the feedback usable with the fixed furniture
of XP1 and the models of XP2 and XP3; the microphone is not opened a second time; the pause tolerance of the
speaker is really applied; the comparison of the three gazes and the writes of network properties outside the
authority are fixed; the collective voice threshold uses the script default (3 interruptions) rather than the
scene value of the branch (1).

## Running a session

1. The three headsets on the same network (see `docs/reseau.md`). To collect the data
   automatically, start the receiver on the experimenter's PC and put its URL in `SessionConfig`
   before building the APKs:

   ```powershell
   python docs/receive_data.py --port 8765 --out .\sessions
   ```

2. Tutorial on each headset ("Passer le tutoriel" button under the progress panel to skip it). On every
   scene load the headset is recentred in front of the starting panel (`startInFrontOfPanel`).
3. Experiment scene: the trackers, voice included, record from the scene load. Pick the role in the list
   (Lecteur, Calculateur, Modélisateur), then "Rejoindre" (join). The counter "Joueurs : n/N" shows the
   connection state (N = `maxPlayers` from `SessionConfig`).
4. Experimenter's headset, if any: A, A, B, B on the right controller before "Rejoindre" (see
   "Experimenter mode").
5. The Modeller (`startActivityRole`) presses "Lancer l'activité" (start the activity): a waiting window appears on each
   headset, then the `MARKER` line is written at the same network time on the three headsets, the
   sync beep is inserted in the audio file and played in the headset, the session manifest is
   written and the activity clock appears on every headset.
6. The Reader (`stopActivityRole`) ends the activity, or the automatic stop occurs after `sessionTimeoutMinutes`:
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
- **Experimenter**: A, A, B, B on the right controller; panel with recording start / stop, forced end of
  session (with confirmation), feedbacks ON / OFF, live role change (back to a participant role included),
  simulated players, real-time monitoring of the recordings.

## Recorded data

On the headset: `Android/data/<application identifier>/files/Data/`.

| File | Content |
| --- | --- |
| `*_Session.json` | manifest: session, role, scene, headset, application version, marker and end times (local, network, UTC), active feedbacks, files produced and their size |
| `*_Audio.wav` | voice, 16-bit PCM mono 44.1 kHz, starts when the scene loads (`audioFromSceneStart`), synchronisation beep inserted when the activity starts (1 kHz, 0.5 s, `Assets/Resources/SyncSignal.wav`) |
| `*_EyeTrackingData.csv` | gaze: origin, hit point, object looked at, at frame rate |
| `*_FaceTrackingData.csv` | 71 facial action units, at frame rate |
| `*_UsersPositions.csv` | positions, 2 Hz |
| `*_ChairePositionData.csv` / `*_ReservationPositionData.csv` / `*_CalepinagePositionData.csv` | end-of-session summary: objects, reservations or equipment from all headsets |
| `*_FeedbackData.csv` | collaborative feedback events, when they were on (see above) |

The gaze, face and position CSV files start with three time columns: `Time` (seconds since the
application was launched on this headset), `NetTime` (Photon Fusion network clock, identical on
the three headsets of a session, -1 outside a session) and `Utc` (headset UTC time, ISO 8601).
Headsets are aligned on `NetTime`. Each CSV contains a `MARKER` line at start and ends with `END`.
The audio file follows real time: if the microphone capture is interrupted (headset removed, system
menu), the missing span is filled with silence so that the position in the file stays aligned with
the CSV files (a warning with the padded duration is logged).
Check with:

```powershell
python docs/check_session.py <Data folder copied from the headset>
```

The analysis of these data (computation and use of the indicators measured during the task) is done by the
**TRACE** pipeline (Traceable Reproducible Analysis of Collective Emergent States):
<https://github.com/Lannuzel/TRACE---Traceable-Reproducible-Analysis-of-Collective-Emergent-States>.
This application produces the files; TRACE processes them.

## Architecture (key points)

- `Assets/Scripts/Core/`: `SessionConfig` (configuration asset), `SessionCore` (base installed by
  code in the scenes), `StartPlacement` (headset recentred in front of the starting panel),
  `SessionClock` (time columns of the CSV files),
  `SessionManifest` (JSON manifest), `DataUploader` (HTTP upload of the files at the end of a session).
- `Assets/Scripts/Network/NetworkManager.cs`: Fusion session, automatic reconnection after a
  connection loss, `GetLocalPlayerName()` for the local role.
- `Assets/Scripts/SyncManager.cs`, `HostController.cs`, `ExitManager.cs` (on the avatar
  `PlayerPrefabMB`): synchronised start and stop, end of session.
- `Assets/Scripts/Network/AdminTools.cs`, `Assets/Scripts/UI/ExperimenterPanel.cs`,
  `ExperimenterShortcut.cs`: experimenter tools and the A, A, B, B sequence.
- `Assets/Scripts/Feedback/`: collaborative feedbacks (`FeedbackManager`, network object
  `FeedbackNetwork`, gaze, voice, face, log, tutorial steps).
- `Assets/Scripts/Undo/UndoManager.cs`: Modeller undo stack (networked Despawn / Spawn /
  teleport; XP3 scaling cannot be undone).
- `Assets/Scripts/Tutorial/TutorialManager.cs`: progression and validation of the tutorial steps;
  `ModeGizmo.cs`: move / rotate gizmo on the tutorial object (same drawing as
  `TranslateGizmoDrawer` / `RotationGizmoDrawer` in XP2).
- `Assets/BIM Resources/Scripts/BIMBarange/`: Modeller tools (selection, measurement, alignment,
  distance to wall, reservations) and the activity clock (`CountDownWatch`).
  `ObjectInteractionHandler.HeightFor` gives the placement height of an object: 0 (floor) in XP1,
  ceiling height by object name in XP3 (`spawnHeightRules`).
- Interfaces created in code: use `XRUiCanvas.MakeInteractive` so that they respond to the Meta
  laser pointer (`OVRRaycaster`).

## Repository rules

The repository only contains what the four build scenes reach, plus the folders loaded by name
(`Resources`, `StreamingAssets`, `Editor`, `Scripts`, SDKs); see `docs/history.md` for the cleanup
that brought it there.

- never ignore `*.meta`: every asset must be versioned with its `.meta`;
- build archives, logs and IDE settings are excluded by `.gitignore`; images and videos go through
  Git LFS (`.gitattributes`);
- a test scene does not go into `Assets/EXP*/Scenes`; the Build Settings list only the tutorial
  and the three experiment scenes;
- never delete a folder wholesale without checking the references into it; run
  `python docs/check_references.py .` after any deletion: it must report no `MANQUANT` line;
- shader include files (`*.cginc`, `*.hlsl`) are referenced by name, not by GUID: a dependency
  analysis does not see them; a magenta avatar or keyboard means an include is missing.

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
