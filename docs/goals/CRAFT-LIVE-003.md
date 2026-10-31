# CRAFT-LIVE-003 — comparaison immédiate des coups de craft

Version 0.45.0.0, 2026-10-29. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL.

## Fonction disponible

Dans **Synthèse en cours**, l'observation reste désactivée par défaut. Une fois activée, le panneau **Comparer les prochains coups** présente pour les actions admissibles les progression, qualité et solidité après le coup, le coût en PS, Calme intérieur et le résultat immédiat (continue, casse, termine sous cible ou atteint la cible du modèle). Le tri est manuel par gain immédiat de qualité/progression ou solidité restante ; il ne choisit pas une rotation viable.

Seule la condition Normale est projetée à ce stade. Une cible de qualité explicite signale une fin prématurée ; elle doit rester entre zéro et le maximum de la recette. Aucun gain HQ garanti, aucune macro ou action lancée, aucune recherche complète à partir de l'état natif.

Si le combo n'est pas établi, les états possibles sont conservés : par exemple coût 18–32 PS pour Standard Touch, 18–46 pour Advanced Touch, ou un intervalle de degrés après Refined Touch. Une utilisation passée de Main suprême encore inconnue reste une alternative. L'admissibilité native et les PS observés éliminent seulement les alternatives impossibles pour le coup considéré ; aucune alternative restante n'est enregistrée comme fait observé. Les intervalles sont des projections du modèle et non un état natif reconstruit.

## Acquisition native et conventions

Le lecteur copie maintenant les valeurs typées d'AddonSynthesis en plus des textes : progression/max, qualité/max, solidité/max, étape, résultat, condition et indicateur expert. Le schéma est borné et chaque valeur doit posséder le type attendu UInt ou Bool. Le RecipeId et les maxima du catalogue doivent concorder avec ces valeurs et les textes ; un champ absent ou incompatible bloque la comparaison. Pour une recette sans qualité, la valeur native explicitement nulle et le maximum nul peuvent être utilisés même si le texte de qualité est masqué.

Deux conventions de condition sont distinctes : CraftEventHandler.CraftCondition.Normal vaut **1**, tandis que l'ordinal d'AddonSynthesis/SynthesisValues pour Normale vaut **0**. Les deux sources sont contrôlées séparément. L'étape UI typée commence à un ; le moteur reçoit cette valeur moins un. L'étape brute du handler reste affichée séparément et sert à vérifier la stabilité de la lecture, sans convention d'index inventée.

Pour les observations normales prêtes, ActionManager.GetActionStatus est appelé en lecture seule sur les 26 IDs du métier, avec ActionType.CraftAction pour les CraftAction et ActionType.Action pour les effets partagés. Zéro est l'admissibilité observée ; les autres codes restent bruts et ne sont pas traduits en une cause supposée. La vérification de recharge est explicitement désactivée, la vérification de lancement conservée : la ligne n'est pas une promesse d'exécution immédiate si un délai de récupération subsiste. GetActionCost n'est pas appelé : ses quatre paramètres additionnels sont insuffisamment documentés, ils n'ont pas été devinés.

Le même cadre contient valeurs et disponibilités copiées. Identité de fenêtre/recette/étape/condition, valeurs typées et PS sont revérifiés avant publication. Aucun pointeur, RowRef ou appel natif dans Draw ; le panneau met le résultat pur en cache par identité de cadre et catalogue. Le cadre périmé au-delà de deux secondes n'est plus présenté comme une projection courante.

## Modèle et refus

CraftLiveProjection utilise la transition CraftSimulation.Apply existante. Il développe au maximum trois combos et deux historiques d'action unique par action admissible, puis conserve les minima/maxima observés. Les effets identifiés sont copiés depuis leurs compteurs Param. L'admissibilité native de Manipulation permet de modéliser son coup sans inventer un déblocage durable du personnage ; cette information n'est pas persistée.

Refus : observation hors propriétaire/activée/période, action ou animation en cours, identité non résolue, recette hors périmètre, condition non normale, maxima/valeurs discordants, liste de statuts ou d'actions incomplète, effet reconnu non modélisé, doublon ou compteur invalide. Une action déclarée admissible côté client mais sans transition compatible est signalée comme incohérence, pas exécutée ou corrigée arbitrairement. Une progression achevée à solidité zéro reste une fin ; une action qui casse avant la fin est clairement signalée.

Les influences connues et les 26 actions du modèle restent le périmètre. Une liste de compteurs interprétables ne certifie pas toutes les influences du jeu. Les recettes spéciales, conditions favorables/mauvaises, bonus d'outil spécifiques, disponibilité serveur réelle et rotations adaptatives natives restent à développer/valider. Les procs bruts de CRAFT-LIVE-002 restent distincts des coûts projetés.

## Preuves

CURRENT_VERIFIED le 2026-10-29 : [Craftimizer SynthesisValues](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Craftimizer/Utils/SynthesisValues.cs), schéma typé ; [Condition](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Condition.cs), ordinal Normal=0 ; SynthHelper pour étape UI moins un ; Hooks pour test GetActionStatus==0. Implémentation originale, aucun hook importé.

[FFXIVClientStructs ActionManager](https://github.com/aers/FFXIVClientStructs/blob/385b0821bc4573b77fbde84c930774eeca8283ec/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs) : getter et paramètres de contrôle, API de coût encore partiellement nommée. LOCAL_VERIFIED : signatures de l'assembly installée, ActionType.Action=1/CraftAction=9, AtkValue.Type/UInt/Bool et CraftCondition.Normal=1 via ApiProbe. Ni ApiProbe ni les probes sqpack n'ont exécuté ces getters dans un client en cours.

COMPILED / TESTED : Verify.ps1 Release 0.45.0.0, 290/290 tests et package/versions/ZIP/assemblies hôtes verts, zéro avertissement/erreur. Sept groupes nouveaux : intervalles combo/charges, disponibilité native et PS, âge/animation/conditions y compris distinction 0/1, effets/protection, fin à zéro/casse, identité/action étrangère/doublons, calcul indépendant de gain et immutabilité. Les 283 tests précédents passent.

GatheringProbe et CraftSimulationProbe exécutés : fixtures natives, douze effets et 208 actions passants. Correction de l'inclusion générique de GatheringProbe : CraftPresets, lié à Configuration et sans rôle dans ce probe, est désormais exclu pour conserver la compilation après 0.44. Cette correction ne supprime aucun contrôle natif utile.

## Essai facultatif demandé, pas déclaré réalisé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.45.0.0. Vérifier en synthèse d'essai ordinaire et condition Normale : projections présentes et chiffres conformes, coût variable après Basic/Standard/Observe, fin sous cible et disparition lors d'une animation/fermeture. Si aucune projection : conserver le motif et les deux codes de condition du panneau brut. L'utilisateur a répondu « Je testerai plus tard » le 2026-10-29 : test différé, ne pas le redemander. Macro Chain reste également reporté selon sa réponse antérieure.

Suite : étendre les conditions ordinaires en vérifiant les bonus d'outil favorables plutôt qu'en les supposant absents ; garder séparées projection immédiate et rotation complète. L'objectif global et les 17 domaines PARTIAL sont conservés, réserve utilisateur 5 % de quota.
