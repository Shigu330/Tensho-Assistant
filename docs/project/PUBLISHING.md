# Publication sur le dépôt officiel Dalamud (D17)

Relevé des règles le 2026-10-31 : [soumission](https://dalamud.dev/plugin-publishing/submission),
[restrictions](https://dalamud.dev/plugin-publishing/restrictions), [politique IA](https://dalamud.dev/plugin-publishing/ai-policy),
[métadonnées](https://dalamud.dev/plugin-development/plugin-metadata),
[considérations techniques](https://dalamud.dev/plugin-development/technical-considerations). Audit du code : PUBLISH-018 (0.156.0).
La décision de soumettre appartient à l'auteur ; l'alternative est un dépôt de plugins personnel (URL ajoutée dans les réglages de Dalamud).

## État de la liste de contrôle

| Exigence | État | Où |
|---|---|---|
| Manifeste JSON : Name, Author, Punchline, Description (obligatoires) | fait (0.156, anglais) | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel laissés à DalamudPackager | fait (0.156) ; contrôlé par `Verify.ps1` | idem |
| Version déterministe | fait : seulement `<Version>` du csproj | `TenshoAssistant.csproj` |
| Fenêtres normales via le système de fenêtres Dalamud | fait (0.156 : carte Tensho dans `WindowSystem`) ; restent en `ImGui.Begin` les panneaux accrochés à une fenêtre du jeu (import de banque, aide du carnet, survol d'objet) | `MapViewWindow.cs` |
| Pas de requête automatique au serveur du jeu | conforme : téléportation, application du tri, chaîne de macros seulement sur clic | ARCHITECTURE |
| Pas d'automatisation (artisanat, dialogues, butin…) | conforme : simulateur et solveur sans exécution | — |
| Pas d'avantage en combat ni JcJ, pas de parser | conforme (ACT retiré, DEC-009) ; la carte se masque en combat sur option | — |
| Identifiants de compte : seulement le sien | conforme : seul `player.ContentId` (personnage local) sert de clé | lecteurs `*.Dalamud.cs` |
| Requêtes réseau minimales, HTTPS, nom d'hôte | conforme : Universalis (monde + objet), Garland Tools, wiki, données Revue de mode ; aucune donnée du joueur envoyée | [REFERENCES](REFERENCES.md) |
| Pas de télémétrie | conforme : aucune | — |
| Icône `images/icon.png` 1:1, 64–512 px | fait (0.157) : 512×512, générée par l'agent IA, **déclarée** dans la description (la politique préfère une icône faite main) | `src/TenshoAssistant/images/icon.png`, `publish/d17/…/images/icon.png` |
| `manifest.toml` dans `testing/live/TenshoAssistant/` | prêt (0.157) ; **hash du commit à renseigner** après push | `publish/d17/testing/live/TenshoAssistant/manifest.toml` |
| Déclaration d'usage de l'IA dans la PR | brouillon (niveau « Auto ») ; **à relire et reformuler par l'auteur** (texte humain exigé) | `publish/d17/PULL_REQUEST.md` |
| L'auteur comprend, a testé et sait expliquer le code | **à l'auteur** ; les soumissions de niveau Copilot/Auto sont examinées à la discrétion des mainteneurs | — |
| Traductions assistées par IA relues par des locuteurs | allemand et japonais partiels, non relus | `Localization/` |

## Modèle de `manifest.toml` (dépôt DalamudPluginsD17, `testing/live/TenshoAssistant/manifest.toml`)

```toml
[plugin]
repository = "https://github.com/Shigu330/Plugins-ff14.git"
commit = "<hash du commit public à construire>"
owners = ["Shigu330"]
project_path = "src/TenshoAssistant"
changelog = "<résumé de la version>"
```

À côté : `images/icon.png` (obligatoire), `images/image1.png` à `image5.png` (facultatifs). Une PR par plugin, une branche par
soumission ; chaque mise à jour = nouvelle PR qui change au moins `commit`. Les nouveaux plugins vont dans `testing/live/`.

## Décisions

Décidé le 2026-10-31 (demande de l'auteur) : nom affiché « Tensho Assistant » ; niveau d'IA « Auto » ; icône générée par IA et déclarée ;
dépôt officiel. Reste à l'auteur : commit + push, hash dans `manifest.toml`, fork de goatcorp/DalamudPluginsD17, copie de
`publish/d17/testing/live/TenshoAssistant/`, PR avec un texte relu et reformulé par lui.
