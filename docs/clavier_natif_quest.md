# Étude : utiliser le clavier natif du Meta Quest Pro (F16)

Statut : note d'étude, rien n'est activé dans l'application.

## Ce qui existe aujourd'hui

- Deux claviers virtuels non natifs coexistent :
  - le **clavier spatial XRI** (`Assets/Samples/XR Interaction Toolkit/3.0.5/Spatial Keyboard`),
    utilisé par le tutoriel (saisie nom / âge) et par le bloc-notes des scènes XP via
    `XRKeyboardDisplayMB` ;
  - le **clavier MRTK NonNativeKeyboard** (`Assets/BIM Resources/MRTK-Keyboard-main`), utilisé
    par les champs de dimensions des réservations et par la saisie de distance (`ShowKeyboard`).
- Un essai `TouchScreenKeyboard.Open` existe dans `dummy/VirtualKeyboard.cs` (non utilisé).
  Sur Quest, `TouchScreenKeyboard` ouvre le clavier système 2D d'Android : il s'affiche hors du
  monde 3D et bloque l'application, ce n'est pas adapté.

## Option native : Meta Virtual Keyboard (OVRVirtualKeyboard)

Le SDK Meta XR v69 (déjà dans le projet, `com.meta.xr.sdk.all`) fournit le **Virtual Keyboard**
natif, rendu par le système dans la scène, manipulable aux mains et aux manettes, avec
correction orthographique et disposition selon la langue du casque (AZERTY si le casque est en
français).

Prérequis et étapes :

1. `OVRManager` → Quest Features → *Virtual Keyboard Support* = Required (ajoute la feature
   `com.oculus.feature.VIRTUAL_KEYBOARD` au manifeste).
2. Ajouter le prefab `OVRVirtualKeyboard` (Packages/com.meta.xr.sdk.core/Prefabs) dans la scène,
   lui donner les ancres des mains/manettes et un `OVRVirtualKeyboardInputFieldTextHandler`
   relié au `TMP_InputField` ciblé (un seul champ actif à la fois : il faut rebrancher le
   handler à chaque sélection de champ, comme le fait `XRKeyboardDisplayMB`).
3. S'abonner à `CommitText` / `Enter` pour valider le champ (équivalent de F13).
4. Le clavier natif n'est pas rendu par Unity : il n'apparaît pas dans les captures Unity et
   ne peut pas être stylisé ; sa position se règle avec `OVRVirtualKeyboard.UseSuggestedLocation`.

## Points d'attention pour l'expérimentation

- Le clavier natif consomme les entrées des manettes pendant qu'il est ouvert : les outils
  (mesure, déplacement) doivent être inactifs tant qu'un champ est en édition.
- Il faut tester que l'eye-tracking et le face-tracking continuent d'être livrés pendant que
  le clavier système est affiché (overlay système).
- Deux claviers (XRI pour le bloc-notes, natif pour les champs courts) resteraient nécessaires
  si le bloc-notes doit rester multiligne et redimensionnable.

## Recommandation

Garder les claviers non natifs pour cette version (F13, F14 et F15 les rendent utilisables),
et faire un prototype du clavier natif sur une scène de test avant de l'intégrer aux scènes XP.
