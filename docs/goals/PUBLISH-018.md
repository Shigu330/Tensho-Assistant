# PUBLISH-018 — préparer la publication Dalamud

ID: PUBLISH-018 — préparer la publication sur le dépôt officiel Dalamud (choix utilisateur 2026-10-31)
OBJECTIVE: Rendre le plugin conforme aux règles D17 (soumission, restrictions, politique IA, métadonnées, considérations techniques)
et lister ce qui reste à l'auteur. Liste de contrôle : [PUBLISHING](../project/PUBLISHING.md).

DONE (0.156.0):
- Carte Tensho dans `WindowSystem` (`MapViewWindow`) ; manifeste anglais à jour, champs du packager retirés, version unique (csproj),
  contrôle dans `Verify.ps1` ; audit restrictions/réseau/identifiants/télémétrie conforme.

PROTOCOLE EN JEU (DLL 0.156.0.0):
1. M ouvre et ferme la carte Tensho ; la croix la ferme (et la carte du jeu hôte) ; Échap ne la ferme pas.
2. Agrandir puis réduire ; opacité et verrouillage (Paramètres › Carte) ; masquage en combat si l'option est active.
3. Quête depuis le journal : la carte s'ouvre centrée sur la cible, comme avant.
4. Installateur de plugins Dalamud : nom, accroche et description anglaise affichés.

DONE (0.157.0): icône 512 px (boîte à outils, générée par l'agent, déclarée) ; nom « Tensho Assistant » ; déclaration IA dans la
description ; dépôt officiel choisi : `publish/d17/testing/live/TenshoAssistant/` (manifest.toml, icône) et brouillon `publish/d17/PULL_REQUEST.md`
(niveau « Auto »).

PROTOCOLE EN JEU (DLL 0.157.0.0): liste des plugins de Dalamud : icône boîte à outils et nom « Tensho Assistant » ; titre de la fenêtre principale.

REMAINING (auteur) : commit + push ; hash du commit dans manifest.toml ; relire et reformuler la PR et la déclaration IA (texte humain exigé) ;
fork de goatcorp/DalamudPluginsD17, copie du dossier `testing/live/TenshoAssistant/`, ouverture de la PR.
VALIDATION: Verify.ps1 vert ; essai en jeu ; PR ouverte par l'auteur.
OUT_OF_SCOPE: ouvrir la PR ou pousser à la place de l'auteur.
