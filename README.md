# BIM Project VR

Application de recherche en réalité virtuelle (CESI LINEACT) : une triade collabore sur une tâche
inspirée du BIM pendant que l'application enregistre voix, regard, expressions faciales et positions
pour étudier l'intelligence collective.

- **Moteur** : Unity 2022.3.56f1 (`ProjectSettings/ProjectVersion.txt`), Asset Serialization en Force Text.
- **Casque** : Meta Quest Pro (eye-tracking et face-tracking requis), Android API 32.
- **Réseau** : Photon Fusion 2.0.3, mode Shared, cloud Photon région `eu` (voir `docs/reseau.md`).
- **Rôles** : Lecteur (consignes, lance et termine l'activité), Calculateur (bloc-notes, calculatrice,
  documents), Modélisateur (création, déplacement, mesure des objets), Expérimentateur (panneau
  d'administration, hors tâche).

## Scénarios

| Scénario | Scène | Tâche |
| --- | --- | --- |
| Tutoriel (commun) | `Assets/EXP1/Scenes/Tutorial/BIM-EXP_Tutorial_V2.unity` | saisie, téléportation, tracer des lignes, déplacer un objet, choix du rôle |
| XP1 | `Assets/EXP1/Scenes/Exp1_V3.unity` | aménagement d'une salle de cours (tables et chaises) |
| XP2 | `Assets/EXP2/Scene/Exp2-V1-09-06-2025_EXP2_User.unity` | réservations pour gaines et canalisations dans murs et sols |

Chaque expérience est un APK distinct : scène 0 = tutoriel, scène 1 = expérience
(`SceneLoader` charge l'index 1 à la fin du tutoriel ou avec « Passer le tutoriel »).

## Branches

- `main` : version intégrée (fusion de `fix/recette-2026-11-05`).
- `BIM_Project_VR---V3.0` : version de référence d'avril 2026 avant la recette.
- `fix/recette-2026-11-05` : corrections de la recette du 5 novembre 2026, un commit par point
  (identifiants `F1`–`F19`, `N1`–`N5` du cahier des charges).

## Documents

| Fichier | Contenu |
| --- | --- |
| `docs/cahier_des_charges.md` | exigences F1–F18, N1–N5, anomalies, points à clarifier |
| `docs/recette_2026-11-05.md` | transcription de la note vocale de test |
| `docs/reseau.md` | réseau requis (ports Photon), vérification avant session, comportement en cas de coupure |
| `docs/clavier_natif_quest.md` | étude du clavier natif Meta (non activé) |
| `docs/check_session.py` | contrôle d'intégrité des fichiers d'une session (voir ci-dessous) |

## Générer les APK

Dans Unity : menu **Outils BIM VR › Générer l'APK XP1 / XP2** (`Assets/Editor/BuildExperiments.cs`).
En ligne de commande, depuis un dossier quelconque :

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.56f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath "<dossier du projet>" -buildTarget Android `
  -executeMethod BuildExperiments.BuildXP1 -outputDir "<dossier de sortie>" -logFile build_xp1.log
```

Même commande avec `BuildExperiments.BuildXP2`. Les deux APK ont des identifiants distincts
(`CollectiveIntelligence.CESI.EXP1` et `.EXP2`) et coexistent sur un casque. Le SDK Android doit
contenir la plateforme 32 (`sdkmanager "platforms;android-32"`). Installation :
`adb install -r <fichier.apk>`.

Les seules erreurs consignées pendant une build viennent du package Meta
(`AvatarBehaviourFusion`, exception sur une propriété `[Networked]`) : elles sont connues et
n'empêchent pas la génération.

## Déroulé d'une session

1. Les trois casques sur le même réseau (voir `docs/reseau.md`).
2. Tutoriel sur chaque casque ; le rôle peut être choisi dès le tutoriel.
3. Scène d'expérience : choisir le rôle dans la liste, « Rejoindre ». Le compteur « Joueurs : n/N »
   affiche l'état de connexion (`MaxPlayers` est réglé sur 1 dans les scènes pour les tests).
4. Le Lecteur appuie sur « Lancer l'activité » : fenêtre d'attente sur chaque casque, puis les
   enregistrements démarrent à la même heure réseau sur les trois casques (marqueur `MARKER`).
5. Le Lecteur termine l'activité, ou l'arrêt automatique survient après 30 minutes :
   fenêtre de remerciement puis fermeture de l'application sur chaque casque.

### Commandes

- **Menu** : bouton X (manette gauche). **Supprimer** : bouton Y. **Annuler la dernière action** :
  bouton dans les menus Édition d'objets, Mesure et Réservation, ou Y maintenu 0,6 s.
- **Fenêtres** (documents, bloc-notes, calculatrice, claviers) : pointer et maintenir la gâchette
  intérieure droite pour les déplacer ; croix en haut à droite pour les fermer.
- **Clavier** : AZERTY, Entrée valide le champ (retour à la ligne uniquement dans le bloc-notes),
  touche « Fermer ».
- **Expérimentateur** : panneau avec lancement / arrêt de l'enregistrement, fin de session forcée
  (confirmation), changement de rôle à chaud, joueurs simulés, suivi en temps réel des enregistrements.

## Données enregistrées

Sur le casque : `Android/data/<identifiant de l'application>/files/Data/`.

| Fichier | Contenu |
| --- | --- |
| `*_Audio.wav` | voix, PCM 16 bits mono 44,1 kHz, démarre au lancement de l'activité, marqueur sonore inséré |
| `*_EyeTrackingData.csv` | regard : origine, point d'impact, objet regardé, au framerate |
| `*_FaceTrackingData.csv` | 71 unités d'action faciales, au framerate |
| `*_UsersPositions.csv` | positions, 2 Hz |
| `*_ChairePositionData.csv` / `*_ReservationPositionData.csv` | bilan de fin : objets ou réservations de tous les casques |

Chaque CSV contient une ligne `MARKER` au lancement et se termine par `END`. Vérification :

```powershell
python docs/check_session.py <dossier Data copié depuis le casque>
```

## Architecture (points clés)

- `Assets/Scripts/Network/NetworkManager.cs` : session Fusion, reconnexion automatique après une
  coupure, `GetLocalPlayerName()` pour le rôle local.
- `Assets/Scripts/SyncManager.cs`, `HostController.cs`, `ExitManager.cs` (sur l'avatar
  `PlayerPrefabMB`) : lancement et arrêt synchronisés, fin de session.
- `Assets/Scripts/Network/AdminTools.cs`, `Assets/Scripts/UI/ExperimenterPanel.cs` : outils de
  l'expérimentateur.
- `Assets/Scripts/Undo/UndoManager.cs` : pile d'annulation du Modélisateur (Despawn / Spawn /
  téléportation sur le réseau).
- `Assets/Scripts/Tutorial/TutorialManager.cs` : progression et validation des étapes du tutoriel.
- `Assets/BIM Resources/Scripts/BIMBarange/` : outils du Modélisateur (sélection, mesure,
  alignement, distance au mur, réservations).
- Interfaces créées en code : utiliser `XRUiCanvas.MakeInteractive` pour qu'elles répondent au
  pointeur laser Meta (`OVRRaycaster`).
