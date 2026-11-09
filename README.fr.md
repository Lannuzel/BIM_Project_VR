# BIM Project VR

*English version: [README.md](README.md).*

![Vue du Modélisateur pendant une passation : mesure entre deux objets, menu de la main gauche, avatars du Lecteur et du Calculateur](docs/images/application.jpg)

Application de recherche en réalité virtuelle (CESI LINEACT) : une triade collabore sur une tâche
inspirée du BIM pendant que l'application enregistre voix, regard, expressions faciales et positions
pour étudier l'intelligence collective. Ci-dessus, deux instants de la vue du Modélisateur en XP1,
extraits d'une minute de passation filmée depuis le casque (`docs/images/5__expe_tristan_lannuzel.mp4`).

- **Moteur** : Unity 2022.3.56f1 (`ProjectSettings/ProjectVersion.txt`), Asset Serialization en Force Text.
- **Casque** : Meta Quest Pro (eye-tracking et face-tracking requis), Android API 32.
- **Réseau** : Photon Fusion 2.0.3, mode Shared, cloud Photon région `eu` (voir `docs/reseau.md`).
- **Rôles** : Lecteur (consignes, termine l'activité), Calculateur (bloc-notes, calculatrice,
  documents), Modélisateur (création, déplacement, mesure des objets, lance l'activité), Expérimentateur
  (panneau d'administration, hors tâche ; rôle caché, obtenu par A, A, B, B sur la manette droite).

<img src="docs/images/avatar.png" alt="Avatar réseau d'un participant (tête, corps, étiquette de rôle et manettes)" width="150" align="right">

Chaque participant est représenté par l'avatar ci-contre (`Assets/Prefabs/PlayerPrefabMB.prefab`) :
tête et corps translucides, deux yeux qui suivent le casque, le rôle affiché au-dessus de la tête et
les deux manettes Touch Pro. L'image est produite par **Outils BIM VR › Capturer l'avatar**
(`Assets/Editor/AvatarSnapshot.cs`).

## Scénarios

| Scénario | Scène | Tâche | État |
| --- | --- | --- | --- |
| Tutoriel (commun) | `Assets/EXP1/Scenes/Tutorial/BIM-EXP_Tutorial_V2.unity` | saisie, téléportation, tracer des lignes, déplacer un objet | testé |
| XP1 | `Assets/EXP1/Scenes/Exp1_V3.unity` | aménagement d'une salle de cours (tables et chaises) | testé (recettes de novembre 2026) |
| XP2 | `Assets/EXP2/Scene/Exp2-V1-09-06-2025_EXP2_User.unity` | réservations pour gaines et canalisations dans murs et sols | testé (recettes de novembre 2026) |
| XP3 | `Assets/EXP3/Scenes/Exp3-V0.2-10-02-26.unity` | équipements au plafond (luminaires, bouches d'air, ventilateurs), mise à l'échelle par rapport à une dalle, taux de brassage, réservations | testé seul au casque, **pas encore de passation à trois** |

Chaque expérience est un APK distinct : scène 0 = tutoriel, scène 1 = expérience
(`SceneLoader` charge l'index 1 à la fin du tutoriel ou avec « Passer le tutoriel »).

L'histoire du projet (branches et étiquettes, intégration de l'XP3, nettoyage du dépôt, recettes) est
dans `docs/historique.md`.

## Documents

| Fichier | Contenu |
| --- | --- |
| `docs/historique.md` | branches, étiquettes, intégration de l'XP3, nettoyage du dépôt, recettes, erreurs de build connues |
| `docs/cahier_des_charges.md` | exigences F1–F18, N1–N5, anomalies, points à clarifier |
| `docs/recette_2026-11-05.md` | transcription de la note vocale de test |
| `docs/reseau.md` | réseau requis (ports Photon), vérification avant session, comportement en cas de coupure |
| `docs/clavier_natif_quest.md` | étude du clavier natif Meta (non activé) |
| `docs/check_session.py` | contrôle d'intégrité des fichiers d'une session (voir ci-dessous) |
| `docs/receive_data.py` | récepteur HTTP qui collecte les fichiers de session des casques (voir ci-dessous) |
| `docs/check_references.py` | liste (et peut restaurer) les assets encore référencés par les scènes, prefabs ou matériaux mais absents du projet ; à lancer après tout nettoyage |
| `docs/images/` | illustrations du README et vidéo d'une passation (Git LFS) |

## Générer les APK

Dans Unity : menu **Outils BIM VR › Générer l'APK XP1 / XP2 / XP3** (`Assets/Editor/BuildExperiments.cs`).
En ligne de commande, depuis un dossier quelconque :

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.56f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath "<dossier du projet>" -buildTarget Android `
  -executeMethod BuildExperiments.BuildXP1 -outputDir "<dossier de sortie>" -logFile build_xp1.log
```

Même commande avec `BuildExperiments.BuildXP2`, `BuildXP3` ou `BuildAll` (les trois). Les APK ont des
identifiants distincts (`CollectiveIntelligence.CESI.EXP1`, `.EXP2`, `.EXP3`) et coexistent sur un
casque. Le SDK Android doit contenir la plateforme 32 (`sdkmanager "platforms;android-32"`).
Installation : `adb install -r <fichier.apk>`.

Avant chaque build, `BuildExperiments` reconstruit la table des prefabs réseau Photon Fusion à partir
des labels `FusionPrefab` et refuse une table suspecte : une table périmée (prefabs supprimés ou
ajoutés sans réimport de `NetworkProjectConfig`) empêche silencieusement toute création d'objet sur
casque. Le menu **Outils BIM VR › Vérifier la table des prefabs Fusion** l'affiche pour diagnostic.

Au build, `StaticBatchingAtBuild` marque *Batching Static* tous les renderers issus d'un modèle FBX
fixe (maquettes BIM de XP2 et XP3, des milliers de nœuds de géométrie) : Unity combine leurs
maillages dans l'APK (les scènes sur disque ne sont pas modifiées). Les icônes des boutons
« Générer … » de XP3 sont rendues à partir des prefabs par **Outils BIM VR › Générer les icônes des
prefabs XP3** (`Assets/Resources/Icone/XP3_*.png`) et affectées par code au chargement de la scène.
Les erreurs de build connues (package Meta) sont listées dans `docs/historique.md`.

## Configuration d'une session

Tous les réglages de session sont dans un seul asset : `Assets/Resources/SessionConfig.asset`. Le
sélectionner **dans Unity** : les champs apparaissent dans l'inspecteur (un champ laissé à sa valeur par
défaut n'est pas écrit dans le fichier `.asset`, il n'apparaît donc pas dans un éditeur de texte). Les
réglages sont rappelés dans le panneau de l'expérimentateur.

| Champ | Rôle | Valeur actuelle |
| --- | --- | --- |
| `sessionName` | nom de la session Photon rejointe par les casques | `Modelisateur` |
| `photonRegion` | région Photon fixe | `eu` |
| `maxPlayers` | joueurs attendus avant de pouvoir lancer l'activité | 1 (tests) ; mettre 3 en passation |
| `startActivityRole` | rôle qui voit la vérification des participants et le bouton « Lancer l'activité » | `Modelisateur` |
| `stopActivityRole` | rôle qui voit « Arreter l'enregistrement » une fois l'activité lancée | `Lecteur` |
| `sessionTimeoutMinutes` | arrêt automatique des enregistrements | 30 |
| `syncLeadSeconds` | avance donnée à l'heure réseau de lancement | 1 |
| `exitDelayAfterStop` | délai avant fermeture après la fenêtre de remerciement | 6 s |
| `audioFromSceneStart` | l'enregistrement vocal démarre au chargement de la scène, comme les autres capteurs (le bip de synchronisation est inséré au lancement de l'activité) | vrai |
| `playSyncBeep` | le bip de synchronisation est aussi joué dans le casque au lancement de l'activité | vrai |
| `startInFrontOfPanel` | au chargement d'une scène, le casque est recentré devant le panneau d'accueil (tutoriel) ou de choix du rôle (expériences), quelle que soit son orientation physique | vrai |
| `startDistance` | distance entre le casque et le panneau de choix du rôle au départ | 1,6 m |
| `startDistanceTutorial` | distance entre le casque et le panneau d'accueil du tutoriel (4 m de large) | 2,2 m |
| `clockPlacements` | position du cadran de l'horloge de l'activité par scène (fragment du nom de scène, position, rotation) ; sans entrée, pose du prefab (tableau blanc de XP1 et XP2) | `Exp3` → (6,50 ; 1,75 ; 1,20), mur est |
| `feedbackGaze` / `feedbackVoice` / `feedbackFace` | feedbacks collaboratifs (voir plus bas) | faux |
| `feedbackScenes` | scènes d'expérience concernées par les feedbacks (vide = toutes) | vide |
| `uploadUrl` | récepteur des données sur le PC (vide = pas d'envoi) | vide |
| `uploadTimeoutSeconds` | délai maximal accordé à l'envoi | 180 |

Le socle commun des scènes (fenêtres déplaçables, clavier, pile d'annulation, recentrage au départ,
mode expérimentateur caché, feedbacks, bloc-notes multiligne) est installé par code au chargement de la
scène (`Assets/Scripts/Core/SessionCore.cs`) : rien à recopier d'une scène à l'autre.

## Feedbacks collaboratifs

Portage des travaux d'Emery Descours (branche `feedback`, août 2026, conservée sous l'étiquette
`archive/feedback` ; voir l'article IHM'26 dans les citations). Trois feedbacks, installés par code dans XP1,
XP2 et XP3 (`Assets/Scripts/Feedback/`), **désactivés par défaut** :

| Feedback | Comportement | Réglages |
| --- | --- | --- |
| Regard (`feedbackGaze`) | quand au moins deux participants regardent le même objet, il reçoit un contour blanc dont l'opacité monte sur 10 s (bleu quand les trois le regardent) | — |
| Parole (`feedbackVoice`) | le niveau du micro de l'enregistrement est lu (seuil en dBFS) ; l'autorité réseau suit l'orateur courant et les interruptions réussies ou ratées. Individuel : micro barré 1 s dans le menu de la main gauche pour celui qui a coupé la parole. Collectif : message à tous (avec un son) quand, sur une fenêtre glissante, le ratio interruptions réussies / tentatives dépasse `voiceInterruptionRatio` et le nombre dépasse `voiceInterruptionCount` | `voiceDbThreshold` (-70), `voiceTimeRequired` (1 s), `voiceWindowSeconds` (60), `voiceInterruptionRatio` (0,62), `voiceInterruptionCount` (3) |
| Visage (`feedbackFace`) | unités d'action 15 (coins des lèvres) et 17 (menton) : une ampoule dans le menu de la main gauche dont l'opacité suit la proportion d'échantillons avec expression sur la fenêtre ; un participant est « concentré » quand la proportion atteint `faceThreshold` ; message à tous (avec un son) quand les trois participants sont non concentrés | `faceWeightThreshold` (0,1), `faceEvaluationSeconds` (60), `faceThreshold` (0,02) |

### Activer ou désactiver les feedbacks

L'activation est la condition expérimentale. Deux moyens, cumulables :

1. **Avant de générer les APK** : dans Unity, sélectionner `Assets/Resources/SessionConfig.asset` et cocher
   `feedbackGaze`, `feedbackVoice` et / ou `feedbackFace` dans l'inspecteur (`feedbackScenes` limite
   éventuellement l'effet à certaines scènes). Les trois casques démarrent alors avec ces feedbacks actifs,
   et le tutoriel gagne deux étapes : une vérification des capteurs (micro et visage) avant l'écran
   d'accueil, et une explication des pictogrammes avant le lancement de l'expérience.
2. **Pendant la session** : sur le casque de l'expérimentateur, suite A, A, B, B sur la manette droite pour
   ouvrir le panneau de l'expérimentateur, puis boutons **Regard**, **Parole**, **Visage** (étiquette
   « ON » / « OFF »). Le choix est propagé aux trois casques par RPC et prend effet immédiatement, avant
   ou après le lancement de l'activité, même si la configuration laissait tout éteint.

La condition est écrite dans le manifeste de session (`feedbackGaze`, `feedbackVoice`, `feedbackFace`) et
chaque bascule est consignée dans le journal des feedbacks. Un objet réseau `FeedbackNetwork` (prefab
généré dans `Assets/Resources/Feedback/` par **Outils BIM VR › Créer le prefab réseau des feedbacks**,
aussi au build) est créé par le maître de session et porte l'état partagé.

Les événements sont consignés dans `*_FeedbackData.csv` (mêmes colonnes de temps, `MARKER`, `END`, listé dans le
manifeste, envoyé avec le reste) : `Feedback` (gaze, voice, face, session), `Event`, `Role`, `Target`, `Value`,
`Count`, `Total`, `Ratio`, `Who`. Le fichier n'est créé que si un feedback a été actif ou basculé pendant la
session.

Écarts documentés par rapport à la version publiée : la détection du regard couvre tout objet à collider
(identifiant stable calculé par code), pas seulement les objets réseau, ce qui rend le feedback utilisable avec le
mobilier fixe de XP1 et les maquettes de XP2 et XP3 ; le micro n'est pas ouvert une seconde fois ; la tolérance de
pause de l'orateur est réellement appliquée ; la comparaison des trois regards et les écritures de propriétés
réseau hors autorité sont corrigées ; le seuil collectif de parole reprend la valeur du script (3 interruptions)
plutôt que celle de la scène de la branche (1).

## Déroulé d'une session

1. Les trois casques sur le même réseau (voir `docs/reseau.md`). Pour récupérer les données
   automatiquement, lancer le récepteur sur le PC de l'expérimentateur et renseigner son URL dans
   `SessionConfig` avant de générer les APK :

   ```powershell
   python docs/receive_data.py --port 8765 --out .\sessions
   ```

2. Tutoriel sur chaque casque (bouton « Passer le tutoriel » sous le panneau de progression). À chaque
   chargement de scène, le casque est recentré devant le panneau de départ (`startInFrontOfPanel`).
3. Scène d'expérience : les capteurs, voix comprise, enregistrent dès le chargement. Choisir le rôle dans
   la liste (Lecteur, Calculateur, Modélisateur), « Rejoindre ». Le compteur « Joueurs : n/N » affiche
   l'état de connexion (N = `maxPlayers` de `SessionConfig`).
4. Casque de l'expérimentateur, s'il y en a un : A, A, B, B sur la manette droite (en moins de 3 s), dans
   le tutoriel ou dans la scène d'expérience, avant « Rejoindre ». Le rôle Expérimentateur est appliqué,
   sélectionné dans la liste et son panneau s'affiche ; la même suite masque ou réaffiche le panneau.
5. Le Modélisateur (`startActivityRole`) appuie sur « Lancer l'activité » : fenêtre d'attente sur chaque casque, puis les
   lignes `MARKER` sont écrites à la même heure réseau sur les trois casques, le bip de synchronisation
   est inséré dans le fichier audio et joué dans le casque, le manifeste de session est écrit et
   l'horloge de l'activité apparaît sur chaque casque.
6. Le Lecteur (`stopActivityRole`) termine l'activité, ou l'arrêt automatique survient après `sessionTimeoutMinutes` :
   message de remerciement dans un panneau qui suit la tête, envoi des fichiers au PC si une URL est
   configurée (état affiché sous le message), puis fermeture de l'application sur chaque casque. Les fichiers restent sur le casque
   dans tous les cas.

### Commandes

- **Menu** : bouton X (manette gauche). **Supprimer** : bouton Y. **Annuler la dernière action** :
  bouton dans les menus Édition d'objets, Mesure et Réservation, ou Y maintenu 0,6 s.
- **Déplacer / tourner un objet** : gâchette intérieure droite pour saisir, bouton A pour changer de
  mode ; des flèches (déplacement) ou des anneaux (rotation) s'affichent sur l'objet, dans le
  tutoriel comme dans les expériences.
- **Fenêtres** (documents, bloc-notes, calculatrice, claviers) : pointer et maintenir la gâchette
  intérieure droite pour les déplacer ; croix rouge en haut à droite pour les fermer (pour la
  calculatrice, petit carré au coin supérieur droit de son corps, son canvas n'étant pas dans le plan
  de sa façade).
- **Clavier** : AZERTY, Entrée valide le champ (retour à la ligne uniquement dans le bloc-notes),
  touche « Fermer ».
- **Expérimentateur** : A, A, B, B sur la manette droite ; panneau avec lancement / arrêt de
  l'enregistrement, fin de session forcée (confirmation), feedbacks ON / OFF, changement de rôle à chaud
  (retour à un rôle de participant compris), joueurs simulés, suivi en temps réel des enregistrements.

## Données enregistrées

Sur le casque : `Android/data/<identifiant de l'application>/files/Data/`.

| Fichier | Contenu |
| --- | --- |
| `*_Session.json` | manifeste : session, rôle, scène, casque, version de l'application, heure du marqueur et de fin (locale, réseau, UTC), feedbacks actifs, fichiers produits et leur taille |
| `*_Audio.wav` | voix, PCM 16 bits mono 44,1 kHz, démarre au chargement de la scène (`audioFromSceneStart`), bip de synchronisation inséré au lancement de l'activité (1 kHz, 0,5 s, `Assets/Resources/SyncSignal.wav`) |
| `*_EyeTrackingData.csv` | regard : origine, point d'impact, objet regardé, au framerate |
| `*_FaceTrackingData.csv` | 71 unités d'action faciales, au framerate |
| `*_UsersPositions.csv` | positions, 2 Hz |
| `*_ChairePositionData.csv` / `*_ReservationPositionData.csv` / `*_CalepinagePositionData.csv` | bilan de fin : objets, réservations ou équipements de tous les casques |
| `*_FeedbackData.csv` | événements des feedbacks collaboratifs, s'ils ont été actifs (voir plus haut) |

Les CSV du regard, du visage et des positions commencent par trois colonnes de temps :
`Time` (secondes depuis le lancement de l'application sur ce casque), `NetTime` (horloge réseau
Photon Fusion, identique sur les trois casques d'une session, -1 hors session) et `Utc` (heure UTC
du casque, ISO 8601). L'alignement entre casques se fait sur `NetTime`. Chaque CSV contient une
ligne `MARKER` au lancement et se termine par `END`. Le fichier audio suit le temps réel : si la
capture du micro est interrompue (casque retiré, menu système), la durée manquante est comblée par du
silence pour que la position dans le fichier reste alignée sur les CSV (un avertissement avec la durée
comblée est consigné). Vérification :

```powershell
python docs/check_session.py <dossier Data copié depuis le casque>
```

## Architecture (points clés)

- `Assets/Scripts/Core/` : `SessionConfig` (asset de configuration), `SessionCore` (socle installé
  par code dans les scènes), `StartPlacement` (casque recentré devant le panneau de
  départ), `SessionClock` (colonnes de temps des CSV),
  `SessionManifest` (manifeste JSON), `DataUploader` (envoi HTTP des fichiers en fin de session).
- `Assets/Scripts/Network/NetworkManager.cs` : session Fusion, reconnexion automatique après une
  coupure, `GetLocalPlayerName()` pour le rôle local.
- `Assets/Scripts/SyncManager.cs`, `HostController.cs`, `ExitManager.cs` (sur l'avatar
  `PlayerPrefabMB`) : lancement et arrêt synchronisés, fin de session.
- `Assets/Scripts/Network/AdminTools.cs`, `Assets/Scripts/UI/ExperimenterPanel.cs`,
  `ExperimenterShortcut.cs` : outils de l'expérimentateur et suite A, A, B, B.
- `Assets/Scripts/Feedback/` : feedbacks collaboratifs (`FeedbackManager`, objet réseau
  `FeedbackNetwork`, regard, parole, visage, journal, étapes de tutoriel).
- `Assets/Scripts/Undo/UndoManager.cs` : pile d'annulation du Modélisateur (Despawn / Spawn /
  téléportation sur le réseau ; la mise à l'échelle XP3 n'est pas annulable).
- `Assets/Scripts/Tutorial/TutorialManager.cs` : progression et validation des étapes du tutoriel ;
  `ModeGizmo.cs` : repère déplacement / rotation sur l'objet du tutoriel (même dessin que
  `TranslateGizmoDrawer` / `RotationGizmoDrawer` de l'XP2).
- `Assets/BIM Resources/Scripts/BIMBarange/` : outils du Modélisateur (sélection, mesure,
  alignement, distance au mur, réservations) et horloge de l'activité (`CountDownWatch`).
  `ObjectInteractionHandler.HeightFor` donne la hauteur de pose d'un objet : 0 (sol) en XP1, hauteur
  de plafond selon le nom de l'objet en XP3 (`spawnHeightRules`).
- Interfaces créées en code : utiliser `XRUiCanvas.MakeInteractive` pour qu'elles répondent au
  pointeur laser Meta (`OVRRaycaster`).

## Règles du dépôt

Le dépôt ne contient que ce que les quatre scènes de build atteignent, plus les dossiers chargés
par nom (`Resources`, `StreamingAssets`, `Editor`, `Scripts`, SDK) ; voir `docs/historique.md` pour le
nettoyage qui l'a amené là.

- ne jamais ignorer `*.meta` : chaque asset doit être versionné avec son `.meta` ;
- les archives de build, journaux et réglages d'IDE sont exclus par `.gitignore` ; les images et vidéos
  passent par Git LFS (`.gitattributes`) ;
- une scène d'essai ne va pas dans `Assets/EXP*/Scenes` ; les Build Settings listent uniquement
  le tutoriel et les trois scènes d'expérience ;
- ne jamais supprimer un dossier en bloc sans vérifier les références qui y pointent ; lancer
  `python docs/check_references.py .` après toute suppression : aucune ligne `MANQUANT` ne doit rester ;
- les fichiers d'include de shaders (`*.cginc`, `*.hlsl`) sont référencés par nom, pas par GUID : une
  analyse de dépendances ne les voit pas ; un avatar ou un clavier magenta signifie qu'un include manque.

## Citer l'application

Code source : <https://github.com/Lannuzel/BIM_Project_VR>.

Si vous utilisez cette application dans un travail de recherche, merci de citer :

> Tristan Lannuzel, Beatrice Biancardi, Mukesh Barange, Emery Descours, Stéphanie Buisine.
> *BIM Project VR : application de réalité virtuelle multi-utilisateur pour l'étude de l'intelligence
> collective.* CESI LINEACT. <https://github.com/Lannuzel/BIM_Project_VR>

La partie « feedbacks » (branche `feedback`), réalisée par Emery Descours, a fait l'objet d'une
publication :

> Emery Descours, Tristan Lannuzel, Beatrice Biancardi, Mukesh Barange, Stéphanie Buisine.
> Étude de systèmes de feedback en réalité virtuelle pour l'amélioration de l'intelligence
> collective. *IHM'26 - 37e Conférence Internationale Francophone sur l'Interaction Humain-Machine*,
> AFIHM ; Université de Lille, octobre 2026, Villeneuve-d'Ascq, France. ⟨hal-05781898⟩ <https://hal.science/hal-05781898/>
