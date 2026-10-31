# GOAL sélectionné

ID: NPC-VENDORS-014 — TESTED ; VALIDATED_IN_GAME: YES (rapport utilisateur 2026-10-31, DLL 0.99.0.0) — CLOSE ; prochain GOAL à choisir
PLUGIN: Item Use / Mappy (nouvel outil « PNJ vendeurs ») + survol
OBJECTIVE: Relier chaque objet aux PNJ qui le vendent (nom, lieu, coordonnées, coût et monnaie en icône), drapeau sur la carte au clic droit, liste courte signalée au survol.

WHY_NOW: Demande utilisateur du 2026-10-30.

CURRENT_STATE: Verify.ps1 (agent, 2026-10-30 20:27) : DLL 0.88.0.0, 509/509, 0 avertissement/erreur. Également non validés en jeu : 0.87 (wiki source principale des monstres), 0.84 (critères de tri), 0.82–0.83 (caches recettes/récolte).

REFERENCES: docs/NPC-VENDORS-014.md ; docs/HOVER-TOOLTIP-013.md ; docs/CATALOG-CACHE-012.md.

FILES: Npcs/NpcVendorCatalog.cs, NpcVendorLoader.Dalamud.cs (nouveaux) ; MainWindow.NpcVendors.cs (nouveau) ; MainWindow.MarketHover.cs, MainWindow.cs, UI/Navigation/MainWindow.CategoryNavigation.cs, Plugin.cs ; en/fr.json ; tests NpcVendorTests, csproj, Program.cs.

OUT_OF_SCOPE: Monnaies codées autres que mémoquartz (non résolues, affichées comme telles) ; boutiques de compagnie libre, loteries, InclusionShop ; présence/prérequis des PNJ.

VALIDATION: RESTE en jeu : protocole de docs/NPC-VENDORS-014.md.

RECOMMENDED_MODEL: LIGHT

QUOTA_CHECKPOINT: Relevé utilisateur 2026-10-30 ~20:15 : 64 % session / 10 % semaine. Nouveau relevé demandé (deux tranches depuis).

DEFINITION_OF_DONE: Verify.ps1 vert (fait) ; rendu en jeu confirmé.
