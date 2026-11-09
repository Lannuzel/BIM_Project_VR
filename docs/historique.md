# Historique du projet

*English version: [history.md](history.md).* Ce document regroupe ce qui relève de l'histoire du dépôt
(branches, intégrations, nettoyages, recettes) ; le [README](../README.fr.md) ne décrit que l'application
telle qu'elle est.

## Branches et étiquettes

- `main` : version intégrée.
- `fix/recette-2026-11-05` : corrections des recettes de novembre 2026, un commit par point
  (identifiants `F1`–`F19`, `N1`–`N5` du cahier des charges pour la première vague), puis intégration de
  l'XP3, nettoyage du dépôt, portage des feedbacks et retours des passations de test.
- Étiquettes :
  - `v3.0-avant-recette` : état d'avril 2026 avant la recette ;
  - `archive/feedback` : système de feedbacks temps réel d'Emery Descours (dossier `1_Emery`, basé sur le
    `main` de décembre 2024), porté par code dans `Assets/Scripts/Feedback/` le 6 novembre 2026 ;
  - `archive/BIM_Interaction`, `archive/BIM_Clean` : anciennes branches de janvier 2025.

## Intégration de l'XP3 (6 novembre 2026)

L'XP3 vient de la branche `Network` (avril 2026, Mukesh Barange). Cette branche avait été enregistrée sans
les fichiers `.meta` : ils ont été reconstruits, avec les identifiants d'origine quand les scènes permettaient
de les retrouver. Deux matériaux (lumière allumée, métal du ventilateur) n'avaient jamais été versionnés : ils
ont été recréés sous leurs GUID d'origine. La scène se construit et a été testée seule au casque, mais aucune
passation à trois n'a encore validé le scénario. Point connu : l'objet `PDFCanvas` de la scène porte un script
manquant (déjà le cas dans XP1 et XP2, sans effet).

## Nettoyage du dépôt (6 novembre 2026)

Le dépôt ne contient plus que ce que les quatre scènes de build atteignent, plus les dossiers chargés par nom
(`Resources`, `StreamingAssets`, `Editor`, `Scripts`, SDK). Ont été retirés : les kits sans rapport avec
l'expérience (plateformer, VRKeys, ShadersCollection, template multijoueur XRI et ses tutoriels d'éditeur,
Netcode for GameObjects et les services multijoueur Unity, bibliothèques PDF iOS / macOS / UWP / WebGL), les
anciennes scènes de test et les scripts que ni une scène, ni un prefab, ni un autre script ne citent. Tout
reste dans l'historique git.

Leçons retenues (devenues des règles du README) : le nettoyage avait aussi retiré les matériaux des lignes et du
cube du tutoriel, le modèle de la salle et des dépendances des prefabs Fusion (objets magenta sur casque,
création d'objets en panne) ; `docs/check_references.py` a été écrit pour les retrouver. Les deux includes de
shaders de l'avatar et du clavier (`TexturedStableFresnelCommon.cginc`, `MixedRealityShaderUtils.cginc`),
référencés par nom et non par GUID, ont été restaurés le même jour.

## Recettes

- **5 novembre 2026** : première recette sur casque (note vocale transcrite dans `recette_2026-11-05.md`,
  exigences dans `cahier_des_charges.md`) : fenêtres déplaçables, clavier AZERTY, pile d'annulation,
  configuration unique `SessionConfig`, colonnes de temps communes, manifeste de session, envoi des données,
  outils de l'expérimentateur, tutoriel guidé.
- **6 novembre 2026** : horloge locale sur chaque casque, rôles de lancement et d'arrêt configurables,
  panneau du tutoriel sur le mur, nettoyage des repères après annulation, journaux des chaises et du
  calepinage, remplissage des coupures audio, matériaux et icônes de l'XP3, combinaison des maillages des
  maquettes au build, portage des feedbacks d'Emery Descours.
- **9 novembre 2026** : position de départ du tutoriel reculée, panneau de progression déplacé pour ne pas
  recouvrir la consigne de déplacement, horloge de l'XP3 posée sur le mur est, choix du rôle retiré du
  tutoriel et rôle Expérimentateur caché (A, A, B, B), feedbacks basculables depuis le panneau même désactivés
  dans la configuration, illustration et allègement du README ; nettoyage : packages inutilisés retirés du
  manifeste (services Unity, ProBuilder, Transport, tutoriels d'éditeur, Collab) avec leurs réglages, sources
  Word du tutoriel sorties de StreamingAssets (elles partaient dans chaque APK).

## Erreurs de build connues

Les seules erreurs consignées pendant une build viennent du package Meta (`AvatarBehaviourFusion`, exception
sur une propriété `[Networked]`) : elles n'empêchent pas la génération. En fin de `BuildAll`, le message
« Timeout after 300 seconds while waiting async operations » apparaît après la production des trois APK.
