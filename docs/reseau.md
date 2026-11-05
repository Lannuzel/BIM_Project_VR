# Réseau requis pour une session (N3)

Dernière mise à jour : 2026-11-05 (recette).

## Architecture réelle

- Multi-utilisateur : **Photon Fusion 2.0.3, mode Shared**, via le cloud Photon (région fixe `eu`).
  Il n'y a pas d'hôte local : chaque casque se connecte au cloud et rejoint la même session.
- Session : nom fixe `Modelisateur` (voir `Assets/Scripts/Network/ConnectionManager.cs`).
  Tous les rôles (Lecteur, Calculateur, Modélisateur) exécutent exactement la même connexion ;
  rien n'est spécifique au rôle Modélisateur côté réseau.
- Paramètres : `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`
  (UDP, repli TCP activé, Name Server activé), `NetworkProjectConfig.fusion` (timeout 10 s).

Conséquence : si un casque « ne marche pas » sur un Wi-Fi donné, c'est que ce réseau bloque
l'accès au cloud Photon, pas un défaut du rôle. Le même casque fonctionne sur un autre réseau.

## Ce que le réseau de passation doit autoriser

| Besoin | Détail |
| --- | --- |
| Accès Internet sortant | obligatoire pour les 3 casques (cloud Photon, région `eu`) |
| UDP sortant | ports **5055, 5056, 5058** et **27000 à 27003** (Name Server, Master, Game) |
| TCP sortant (repli) | ports **4530 à 4533** (utilisé automatiquement si l'UDP est bloqué) |
| DNS | résolution de `*.exitgames.com` / `*.photonengine.io` |
| Isolation client (AP isolation) | sans effet sur Fusion cloud, mais à désactiver si un usage LAN est envisagé plus tard |
| Portail captif | à éviter : les casques ne peuvent pas valider la page de connexion de façon fiable |

Recommandation : un routeur dédié (ou partage de connexion 4G/5G) auquel seuls les 3 casques
et le PC de l'expérimentateur sont connectés. Les réseaux d'établissement filtrent souvent
l'UDP et imposent un portail captif.

## Vérification avant une session

1. Connecter les 3 casques au même SSID ; vérifier l'heure et que le navigateur du casque
   charge une page.
2. Lancer l'application sur un casque, choisir un rôle, « Rejoindre » : le compteur doit
   passer à `Joueurs : 1/N` en moins de 10 s. S'il reste sur « Connexion… », le réseau bloque
   Photon (voir le message d'erreur affiché devant la caméra).
3. Répéter avec les deux autres casques : le compteur doit atteindre `3/N`.

## Comportement en cas de coupure (N4)

- Un panneau « Connexion réseau perdue » s'affiche devant la caméra, puis l'application
  tente 5 reconnexions espacées de 4 s (réglable sur `NetworkManager`).
- Les enregistrements locaux (voix, regard, visage, positions) continuent pendant la coupure.
- Les objets réseau créés par le casque déconnecté (lignes, mobilier, réservations) peuvent
  disparaître chez les autres participants : c'est le fonctionnement du mode Shared quand un
  joueur quitte la session.
- Si les 5 tentatives échouent, le message demande de prévenir l'expérimentateur ;
  les fichiers de données restent complets sur le casque.
