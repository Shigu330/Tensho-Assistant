# CRAFT-CONDITIONS-004 — 0.46.0, 2026-10-29

La comparaison immédiate accepte les quatre conditions ordinaires : Normale, Favorable, Excellente et Mauvaise. Elle utilise la même transition que le simulateur : facteurs qualité 100/150 ou 175/400/50 %, avant un seul arrondi final. Progression, PS, solidité et Calme intérieur ne changent pas avec ce facteur. Main preste impose directement la qualité maximale. Simulation de rotations, recherche et export gardent leur scénario normal ; aucune rotation native adaptative n'est revendiquée.

## Bonus d’outil et inconnus

ItemCatalog partage les métadonnées des outils principaux des métiers 8–15. Le chargeur lit la description anglaise, ItemSpecialBonus et son paramètre au cours du même chargement borné (128 lignes/budget doux 2 ms). Le texte anglais exact connu du bonus favorable identifie ×1,75 seulement sans autre bonus déclaré. Une description explicitement vide sans bonus ni paramètre identifie ×1,50. Métadonnées absentes, descriptions différentes et effets supplémentaires restent inconnus ; une erreur de feuille anglaise ne bloque pas le catalogue général.

Le lecteur capture l’objet équipé au slot MainHand 0, dans le conteneur chargé EquippedItems de taille bornée 1–32, sur le thread framework. Il revérifie l’identité après copie et ne conserve aucun pointeur. L’ID observé reste disponible si ses métadonnées manquent ; métier inconnu = 0, jamais celui du personnage supposé. Les textes UI visibles sont décodés avec SeString.Parse(...).TextValue, longueur et hiérarchie bornées.

Si le bonus favorable de l’outil n’est pas établi pour le métier observé, seules les qualités affectées deviennent nulles (« Inconnue »). Les autres ressources restent projetées. Une fin avec qualité inconnue n’est jamais présentée comme cible atteinte. Un facteur interne de travail ne devient pas une qualité affichée. Les autres conditions n’exigent pas ce bonus.

## Preuves exécutées

- COMPILED / TESTED : Verify.ps1, Release 0.46.0.0, 295/295 tests et contrôles package/versions/ZIP/exclusion des assemblies hôtes passants, zéro avertissement et erreur.
- Cinq groupes supplémentaires : facteur et arrondi, métadonnées inconnues, favorable standard/amélioré/inconnu, quatre conditions/codes contradictoires/expert, Main preste et condition invalide.
- ToolBonusProbe sur les fichiers sqpack installés, même classification que la production : 928 outils principaux, 273 sans déclaration de bonus, 136 avec ×1,75, 519 inconnus ; zéro marqueur favorable hors outil principal d’artisan. Les cas inconnus ne sont pas promus standards.
- LOCAL_VERIFIED : ApiProbe confirme Parse(ReadOnlySpan<byte>), TextValue et MainHand=0 dans GearsetItemIndex. Les getters d’inventaire ne sont pas exécutés dans ce probe.

CURRENT_VERIFIED : [Craftimizer Gearsets](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Craftimizer/Utils/Gearsets.cs) pour le marqueur anglais ; [simulateur](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Simulator.cs) pour les facteurs ; [Condition](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Condition.cs) pour les ordinaux. Implémentation originale, aucune logique native d’exécution copiée.

VALIDATED_IN_GAME : non. Outil réellement équipé, conditions et projections restent à confirmer en jeu. L’utilisateur a différé le test projection le 2026-10-29 et celui de Macro Chain auparavant : ne pas les redemander. La DLL canonique est C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Les 17 domaines restent PARTIAL et le goal global actif.

Suite : enveloppe conservatrice de qualité d’une séquence fixe, après preuve des transitions ordinaires. Ne pas confondre cette enveloppe avec une probabilité HQ ou une rotation adaptative.
