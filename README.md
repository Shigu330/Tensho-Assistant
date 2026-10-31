# Tensho--Assistant

Audit complémentaire de la livraison 0.59 : [périmètre, liens, DLL/ZIP et limites](docs/BASE-AUDIT-002.md).

Depuis 0.76.0 (2026-10-30) : les quêtes proposant un objet affichent la progression du même relevé (en cours, terminée confirmée, non confirmée) avec filtre ; aucune disponibilité déduite. Voir [QUEST-ITEM-PROGRESS-007](docs/QUEST-ITEM-PROGRESS-007.md).

État 0.75.0 (2026-10-29) : [cache partagé et péremption réglable](docs/CACHE-001.md). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

Nouveau plugin Dalamud All-in-One. `/tensho` ouvre la fenêtre et sa configuration. L'inventaire commun dispose d'un adaptateur pour les quatre sacs du personnage et d'un diagnostic par identifiant d'objet/NQ-HQ. La couverture complète ou partielle est explicite ; les 17 modules métier restent dans la roadmap.

Version 0.1.1 : chaque sac est vérifié chargé et entièrement lu avant de le déclarer complet. Si les quatre sacs passent les contrôles, la quantité sélectionnée est un total exact dans cette portée ; sinon elle reste inconnue ou minimale. Les rangements extérieurs aux quatre sacs restent exclus. La validation live de cette nouvelle vérification reste à effectuer après rechargement de la DLL compilée.

Depuis 0.1.2 : deux options indépendantes d'ouverture automatique, au chargement du plugin et à la connexion du personnage. La nouvelle option de connexion est initialement décochée. L'utilisateur a confirmé les sacs complets, les totaux et le comportement des options ; voir les preuves et limites dans TESTING.md.

Depuis 0.1.3 : NQ, HQ et total NQ + HQ sont affichés ensemble pour l'objet recherché, sans basculer de filtre. L'exactitude reste limitée aux quatre sacs entièrement lus.

Depuis 0.1.4 : recherche par nom ou ID dans le catalogue de la langue du jeu. Attendre le chargement, saisir par exemple `sirop bouleau`, puis sélectionner l'objet pour afficher ses quantités. La casse et les accents sont ignorés ; les résultats sont limités à 30. Les trois quantités NQ/HQ/total restent visibles ensemble. Le catalogue est partagé avec les futurs domaines d'artisanat, récolte et inventaire.

Depuis 0.2.0 : navigation latérale avec Accueil, Objets & inventaire, Tous les outils, Réglages et Diagnostics. L'objet sélectionné reste partagé entre les pages ; les préférences et détails techniques ont leur propre espace. Après rechargement, ouvrez Objets & inventaire pour retrouver la recherche.

Depuis 0.2.1 : Tous les outils présente une grille d'icônes et de noms. Cliquez sur une icône pour ouvrir sa fiche : description, domaine et disponibilité. Les 17 outils futurs sont conservés et leurs fiches indiquent À venir. Le filtre recherche aussi par domaine.

Depuis 0.3.0 : banque persistante pour les sept pages de chaque servant, la sacoche chocobo standard et son extension si disponible. Ouvrez l'inventaire concerné et utilisez le panneau Tensho attaché « Importer / actualiser ». Chaque import remplace le précédent du même rangement ; les relevés sont séparés par personnage et datés. L'inventaire servant équipé, les cristaux et les objets en vente restent exclus. Les caches ne s'actualisent pas automatiquement et peuvent devenir périmés après un transfert.

Historique 0.5.27 : préparation de fabrication avec quantité cible, rendement, ingrédients requis, stocks et liste copiable. Carnet Utilisé dans avec filtres métier et fiche détaillée. Banque d'objets avec ordre visuel du jeu et filtres masquant les sacs sans résultat. Les anciens imports demandent une actualisation pour récupérer les positions.

La banque est enregistrée dans InventoryBank.json du dossier de configuration du plugin (avec une sauvegarde .bak). Ne pas confondre un cache importé complet à sa date avec un inventaire en direct. L'ancrage et les imports natifs de cette version attendent leur validation en jeu ; voir TESTING.md.

## Compiler et vérifier

Depuis PowerShell dans ce dossier :

```powershell
.\scripts\Verify.ps1
# Sur une autre machine :
.\scripts\Verify.ps1 -DotNet 'C:\chemin\dotnet.exe'
```

Prérequis : SDK .NET 10.0.403, Dalamud.NET.Sdk 15.0.0, installation Dalamud API 15. Le script utilise le SDK local vérifié dans `C:\ia-code\.tools\dotnet10`. Le SDK Dalamud résout les assemblies dans `%APPDATA%\XIVLauncher\addon\Hooks\dev`; `DALAMUD_HOME` permet une autre installation.

DLL : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`.
Paquet : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`.
Le projet impose ce dossier même lors d'un build direct du csproj. L'ancien dossier `C:\Plugins-ff14\src\TenshoAssistant\bin\Release` n'est plus une sortie de build valide.

## Essai en jeu à effectuer

Ajouter le chemin absolu de la DLL à la liste des plugins de développement de Dalamud, charger TenshoAssistant puis saisir `/tensho`. Tester la préférence, le rechargement et le déchargement selon TESTING.md. Ne pas interpréter le build comme une validation en jeu.

## Reprendre le développement

Lire RULES.md, STATUS.md, GOAL.md, puis uniquement les fichiers utiles. ROADMAP.md contient les 17 références prioritaires et leur couverture. REFERENCES.md distingue les références historiques des vérifications locales.
