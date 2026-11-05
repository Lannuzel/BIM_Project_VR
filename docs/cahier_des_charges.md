# Cahier des charges – Application VR collaborative (XP1 & XP2)

Nov 5, 2026 · @Tristan

## Contexte et objectifs

L'application est un outil de recherche : elle place une triade en réalité virtuelle sur une tâche collaborative et enregistre ses traces comportementales pour mesurer l'intelligence collective comme un processus dynamique.

- **Cadre** : thèse de doctorat sur l'intelligence collective en RV (CESI LINEACT).
- **Principe de la tâche** : trois rôles complémentaires et interdépendants se partagent l'information ; aucun ne peut réussir seul, la communication vocale est donc indispensable.
- **Inspiration** : situations pédagogiques du BIM (Building Information Modeling), centrées sur la coordination de conception et le raisonnement spatial.
- **Objectif du présent cahier des charges** : fixer les exigences des deux scénarios (XP1 et XP2) et lister les corrections issues de la recette du 5 novembre 2026, pour guider les modifications du dépôt.

## Périmètre et plateforme

L'application vise une collaboration synchrone et co-localisée de trois personnes, chacune équipée d'un casque Meta Quest Pro, dans une session réseau partagée.

| Élément | Choix |
| --- | --- |
| Moteur | Unity 2022 |
| Réseau multi-utilisateur | Photon Fusion |
| Casque | Meta Quest Pro (eye-tracking et suivi facial requis) |
| Configuration | 3 participants + 1 expérimentateur, même salle physique |
| Scénarios | XP1 et XP2, même structure applicative (à confirmer) |
| Durée de tâche | 30 minutes (XP1) |
| Langue de l'interface | Français |

L'environnement est volontairement figé : propriétés des objets, contraintes spatiales et interactions sont identiques d'une session à l'autre, pour garantir la comparabilité des données.

## Rôles et utilisateurs

Chaque participant détient une source d'information que les autres n'ont pas ; le rôle est attribué dans l'application, indépendamment de la position dans l'espace virtuel.

| Rôle | Responsabilité | Source d'information | Outils dans l'application |
| --- | --- | --- | --- |
| Lecteur | Interpréter et transmettre les règles et contraintes ; lance et termine l'activité | Liste des règles d'aménagement | Visualiseur de documents, bouton de lancement |
| Calculateur | Appliquer les règles numériques, calculer dimensions et distances | Tableau de variables et d'équations | Bloc-notes, calculatrice, clavier virtuel, visualiseur de documents |
| Modélisateur | Réaliser l'aménagement : placer, tourner, mesurer les objets | Guide de modélisation | Manipulation d'objets (déplacement, rotation), aides de mesure |
| Expérimentateur (admin) | Superviser la session sans intervenir dans la tâche | — | Fenêtre d'administration, commandes supplémentaires (à créer) |

Tous les participants accèdent aux outils par un menu fixé au-dessus de la manette gauche, et se déplacent par téléportation. Chaque participant est représenté par un avatar minimal, sans expression faciale, portant le nom de son rôle.

## Scénarios expérimentaux

Les deux scénarios partagent les trois rôles et la même architecture réseau, mais diffèrent par la tâche et par les outils du Modélisateur. XP1 correspond à la session S1 (aménagement d'une salle de cours), XP2 à la session S2 (réservations pour réseaux).

### XP1 (S1) — Aménagement d'une salle de cours

**Objectif** : placer un maximum de tables dans la salle de cours en respectant les consignes d'aménagement. Les rôles sont Consignes (Lecteur), Calculs et tableau des variables (Calculateur), Opérateur (Modélisateur).

**Consignes du Lecteur** (L = longueur de la table, p = profondeur ; w, x, y, z fournis au Calculateur) :

| N° | Règle |
| --- | --- |
| 1 | Disposition en rangées |
| 2 | Première table de chaque rangée éloignée du tableau de p × w |
| 3 | Rangées espacées de L × x |
| 4 | Dans une rangée, distance entre deux tables de p × y |
| 5 | Rangée la plus proche des fenêtres éloignée de la longueur du battant + z % |
| 6 | Même nombre de tables dans chaque rangée |
| 7 | Zone d'ouverture de la porte dégagée |

**Outils du Modélisateur** (menu main gauche, gâchette extérieure) :

- **Mesure** : mesure à main levée, mesure entre deux objets (aimantée, axes X ou Z), modifier un repère, supprimer la dernière mesure.
- **Édition d'objets** : sélectionner (cube bleu), déplacer au joystick droit, générer un ensemble chaise + bureau, copier-coller, supprimer, déplacer avec référence (distance saisie en m), afficher la distance aux murs et aux autres objets, aligner sur X ou sur Z (premier objet = référence).

**Déroulé de la session** :

1. Briefing standardisé : objectifs, rôles, modalités d'interaction.
2. Attribution des rôles par discussion, puis saisie dans l'application.
3. Entrée en RV, à distance physique de sécurité.
4. Tutoriel de familiarisation : téléportation, outils, éléments d'interface propres au rôle.
5. Le Modélisateur retire les objets initiaux, puis le Lecteur appuie sur « Lancer l'activité ».
6. Message d'attente jusqu'à la connexion de tous les joueurs.
7. Tâche de 30 minutes.
8. Le Lecteur termine l'activité ; questionnaires post-tâche sur ordinateur.

### XP2 (S2) — Réservations pour le passage des réseaux

**Objectif** : placer dans la structure (murs et sols) les réservations nécessaires au passage des gaines et canalisations. Les rôles sont Consignes (Lecteur), Calculs (Calculateur), Mesure, choix des dimensions et placement (Modélisateur). Le déroulé de session est supposé identique à XP1.

**Consignes du Lecteur** (les coefficients sont fournis au Calculateur ; x et y diffèrent selon que la canalisation traverse un mur ou un sol) :

| Élément | Condition | Dimension de la réservation |
| --- | --- | --- |
| Canalisation | D < 5 cm | D × x |
| Canalisation | D > 5 cm | D × y |
| Canalisations groupées | Écart bord à bord < 15 cm | Rectangulaire : longueur = distance première–dernière + A cm ; hauteur = plus grand diamètre + A cm |
| Gaine, diamètre | D < 10 cm / > 10 cm | D × w / D × x |
| Gaine, longueur | L < 10 cm / > 10 cm | L × y / L × y' |
| Gaine, hauteur | H < 10 cm / > 10 cm | H × z / H × z' |
| Gaines groupées | Écart bord à bord < 15 cm | Rectangulaire : longueur = distance première–dernière + B cm ; largeur = plus grand diamètre + B cm |

L'épaisseur de chaque réservation est égale à celle de son hôte (mur ou sol).

**Outils du Modélisateur** :

- **Mesure** : mesure à main levée, modifier un repère, supprimer la dernière mesure (pas de mesure entre deux objets).
- **Réservation** : réservation circulaire ou rectangulaire, sur mur ou sur sol ; saisie des dimensions puis placement (joystick droit = déplacer, bouton A = basculer rotation / déplacement, relâcher la gâchette = valider) ; supprimer la dernière réservation.

**Exigences propres à XP2** :

- La réservation crée un vide dans le solide hôte (opération booléenne visible).
- Liste des consignes consultable dans l'environnement virtuel.
- Outil de déplacement en marche ou en vol, pour atteindre sols et hauteurs de mur.

## Exigences fonctionnelles

Ces exigences reprennent l'existant et les évolutions demandées lors de la recette ; « S » = souhaitable, non bloquant.

| ID | Domaine | Exigence | Priorité |
| --- | --- | --- | --- |
| F1 | Tutoriel | Illustrer l'emplacement de la commande dans le menu de la main gauche | Must |
| F2 | Tutoriel | Afficher la progression : liste des étapes, grisées tant qu'elles ne sont pas validées | Must |
| F3 | Tutoriel | Vérifier chaque saisie ; afficher un avertissement à côté d'un champ vide ou invalide | Must |
| F4 | Tutoriel | Rendre l'étape « tracer une ligne » claire et fiable ; valider uniquement si la ligne est sur le bloc | Must |
| F5 | Tutoriel | Clarifier l'étape « déplacer un objet » et ses conditions de validation | Must |
| F6 | Tutoriel | Indiquer si l'objet saisi est en mode rotation ou en mode déplacement | Must |
| F7 | Navigation | Garder en permanence un panneau mural expliquant la téléportation | Must |
| F8 | Manipulation | Accélérer la rotation des objets | Must |
| F9 | Menu | Conserver le menu au-dessus de la manette gauche (existant) | Must |
| F10 | Fenêtres | Permettre de déplacer chaque fenêtre (documents, bloc-notes, calculatrice, clavier) avec la gâchette | Must |
| F11 | Fenêtres | Ajouter une croix de fermeture en haut à droite de chaque outil | Must |
| F12 | Fenêtres | Afficher bloc-notes et calculatrice en même temps | S |
| F13 | Clavier | La touche Entrée valide le champ au lieu d'insérer un retour à la ligne | Must |
| F14 | Clavier | Remplacer « Clear » par le bouton de fermeture du clavier ; rendre le clavier plus lisible | Must |
| F15 | Clavier | Disposition AZERTY | S |
| F16 | Clavier | Étudier l'usage du clavier natif du Meta Quest Pro | S |
| F17 | Lancement | Corriger le compteur de joueurs connectés, qui démarre à 0 au lieu de 1 (transcription à confirmer) | Must |
| F18 | Admin | Ajouter des commandes supplémentaires réservées à l'expérimentateur | Must |

## Exigences non fonctionnelles et collecte de données

La valeur scientifique de l'application dépend de la stabilité réseau et de l'intégrité des enregistrements : une session perdue est une triade perdue.

### Collecte de données

Chaque casque enregistre, de façon synchronisée et horodatée :

| Modalité | Contenu | Fréquence |
| --- | --- | --- |
| Regard | Vecteurs 3D : origine, point d'impact, identifiant de l'objet regardé | 74 Hz |
| Expressions faciales | 71 unités d'action (FACS) | 74 Hz |
| Audio | Voix de chaque participant | Continu |
| Comportement spatial | Position et orientation dans l'espace | À préciser |
| Actions | Journal des actions dans l'application | Événementiel |

- **N1** — L'enregistrement audio ne démarre qu'au lancement de l'activité ; aucune activation du micro à l'ouverture de l'application.
- **N2** — Pour chaque rôle et chaque casque, contrôler l'intégrité des fichiers voix, regard et position après une session test.

### Réseau et robustesse

- **N3** — Le rôle Modélisateur doit fonctionner sur le réseau Wi-Fi de passation ; documenter le réseau requis et ses autorisations.
- **N4** — Un changement de réseau en cours de session ne doit pas faire planter l'application, ou doit au minimum être détecté et signalé.
- **N5** — Vérifier la synchronisation multi-utilisateur sur XP1 et XP2.

## Anomalies relevées lors de la recette du 5 novembre 2026

La recette a relevé 7 anomalies ; le crash au changement de réseau et l'activation du micro sont les plus critiques pour la collecte.

| Anomalie | Rôle / zone | Gravité | Exigence liée |
| --- | --- | --- | --- |
| Crash de l'application après un changement de réseau | Global | Bloquante | N4 |
| Micro activé dès l'ouverture de l'application | Calculateur (à vérifier sur tous) | Majeure | N1 |
| Modélisateur inutilisable hors d'un Wi-Fi précis | Modélisateur | Majeure | N3 |
| Entrée insère un retour à la ligne au lieu de valider | Clavier | Majeure | F13 |
| La ligne du tutoriel peut apparaître hors du bloc et l'étape n'est pas validée | Tutoriel | Majeure | F4 |
| Compteur de joueurs connectés à 0 au lieu de 1 | Lancement | Mineure | F17 |
| Étape « déplacer un objet » peu claire | Tutoriel | Mineure | F5 |

Fonctionnent correctement : rôle Lecteur, bloc-notes, calculatrice, clavier (hors touche Entrée), visualiseur de documents.

## Points à clarifier

- [ ] Confirmer la durée de la tâche XP2 et son déroulé (supposés identiques à XP1).
- [ ] Fixer la fréquence d'enregistrement des positions.
- [ ] Décider si l'affichage simultané bloc-notes et calculatrice entre dans cette version.

Incohérences relevées dans les fiches, à trancher avant de coder les validations :

- [ ] XP2 : les cas D = 5 cm (canalisations) et D, L ou H = 10 cm (gaines) ne sont couverts par aucune règle.
- [ ] XP2 : le coefficient x sert à la fois aux canalisations et au diamètre des gaines ; confirmer qu'il s'agit bien de la même variable.
- [ ] XP2 : la réservation groupée a une « hauteur » pour les canalisations et une « largeur » pour les gaines ; harmoniser.
- [ ] XP2 : une gaine a-t-elle un diamètre, ou une longueur et une hauteur ? La fiche donne les trois.
- [ ] XP2 : la fiche Modélisateur demande longueur, largeur et hauteur pour une réservation circulaire ; il faudrait diamètre et épaisseur.
- [ ] XP1 : la fiche Modélisateur indique « icône Aligner X » pour l'alignement sur Z (coquille probable).
