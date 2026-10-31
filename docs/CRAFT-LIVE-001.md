# CRAFT-LIVE-001 — observation expérimentale de synthèse

Version 0.40.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL. Cette tranche observe des données ; elle ne reconstitue pas encore un état complet pour les conseils en cours de craft.

Extension 0.41 : [CRAFT-LIVE-002](CRAFT-LIVE-002.md) identifie douze statuts et leurs compteurs Param, avec icônes et couverture. Les limites de lecture brute et les preuves 0.40 ci-dessous restent historiques ; aucune conversion complète en conseil natif ajoutée.

## Accès et informations

Accueil & outils → Craftimizer → **Observer la synthèse en cours (expérimental)**, ou **Synthèse en cours** depuis le simulateur. L'observation est désactivée par défaut et l'activation reste temporaire pour cette session du plugin. Aucun réglage existant n'est modifié.

Quand la fenêtre Synthesis est prête et visible, copier sur le framework à cinq lectures maximum par seconde : RecipeId/ItemResult/CraftType, étape et condition natives, drapeaux, PS courants et maximums, métier/niveau/Habileté/Contrôle, textes de progression/qualité/solidité et maximums, noms/compteurs des neuf lignes d'effets et indication de débordement, statuts du personnage sous forme ID/Param/RemainingTime bruts.

Les textes sont lus uniquement dans des nœuds et ancêtres visibles, avec profondeur et longueur bornées. Les valeurs numériques acceptent les entiers non signés et groupes de milliers stricts ; chaîne vide, nombre mal formé, pourcentage, décimal ou dépassement restent inconnus. Zéro n'est affiché que s'il est effectivement lu et valide. Une valeur courante au-dessus de son maximum UI reste inconnue ; le texte brut est conservé.

La recette n'est liée au catalogue que si RecipeId, objet résultat, CraftType et métier sont cohérents. Aucun rapprochement par nom, aucun choix de la première variante d'un résultat. Le bouton de contexte ouvre Craft lié pour l'objet résultat ; il ne copie pas la synthèse en cours comme état initial du simulateur.

L'UI affiche la date UTC de copie, les valeurs partielles et l'étape native séparée de l'étape textuelle. Les deux conventions d'étape ne sont pas présumées équivalentes. La condition affiche son texte et son code brut. Les drapeaux d'exécution connus empêchent d'utiliser le bouton de contexte pendant une action/animation ; ils ne prouvent pas à eux seuls la stabilité sémantique de tous les champs.

## Limites explicites

Pas de prochain coup conseillé, de solveur sur l'état natif, d'exécution, de hook d'action ou d'écriture mémoire. Les neuf lignes visibles peuvent être incomplètes ; leur absence ne prouve pas l'absence d'effet. Les valeurs Param/RemainingTime des statuts ne sont pas converties en tours ou charges sans preuve. L'historique d'utilisation de Main suprême, les effets exacts et les combos ne sont pas encore reconstruits.

Les conditions natives énumérées sont disponibles dans les métadonnées locales, mais le modèle de simulation reste normal. Une condition Bonne/Excellente/Médiocre ou experte ne doit pas être traitée silencieusement comme Normale. Les synthèses ajustées/expertes/spéciales et leurs paramètres ne sont pas promues dans le moteur par cette observation.

Les données ne sont ni sauvegardées ni envoyées. Les états désactivé, hors connexion, fenêtre absente, masquée/en transition, observation et erreur sont distincts. Une observation âgée de deux secondes est signalée ancienne et le bouton de contexte est désactivé.

## Propriété et bornes

CraftLiveReader possède l'abonnement framework et le service. Il vérifie propriétaire, présence/visibilité, identité de fenêtre, handler de synthèse et stabilité recette/étape/condition avant publication ; il revérifie aussi le personnage local et son métier. Pas de lecture native dans Draw ni de pointeur/RowRef différé. Les structures de lignes d'effets restent locales au callback ; seules des valeurs managées copiées quittent la lecture.

CraftLiveService publie atomiquement un cadre immuable sous lease/génération. Désactivation, changement de personnage, déconnexion et disposal invalident les leases ; les publications tardives sont rejetées. Fenêtre absente/masquée ou erreur effacent cadre et date antérieurs. Une observation invalide efface aussi le cadre précédent. Une erreur retarde la prochaine tentative de cinq secondes ; Dispose désabonne et désactive.

Bornes : neuf lignes d'effets, 60 statuts parcourus, hiérarchie de texte de 32 nœuds maximum, 2 048 octets de texte natif avant copie, 512 caractères par champ copié. Le lecteur ne parcourt pas un tableau natif par un index supposé et ne lit pas les offsets d'un ancien plugin.

## Preuves

LOCAL_VERIFIED via tools/ApiProbe sur les assemblies installées : EventFramework.Instance/GetCraftEventHandler ; CraftEventHandler RecipeId/ItemResult/CraftType/StepNumber/Condition/CraftFlags/StateFlags ; AddonSynthesis champs textuels nommés et neuf CraftEffect ; AtkTextNode/AtkResNode pour texte/visibilité/parents ; ICharacter CurrentCp/MaxCp et IBattleChara.StatusList. Utf8String.Length/ToString vérifiés. Les enums locaux énumèrent Normal=1, Good=2, Excellent=3, Poor=4 et les états experts ; flags d'exécution 1 et 8. Cela prouve la disponibilité des accès, pas la validité d'une capture en jeu.

Une tentative initiale de trouver CraftingManager a échoué : ce type n'existe pas dans les assemblies locales. L'implémentation utilise le CraftEventHandler effectivement présent. IPlayerCharacter se trouve dans Objects.SubKinds et les PS sont hérités de ICharacter ; aucun nom d'API supposé n'a été conservé.

COMPILED / TESTED : Verify.ps1 Release 0.40.0.0, 265/265 tests, package/versions/ZIP/assemblies hôtes verts ; zéro avertissement/erreur. Cinq nouveaux groupes couvrent nombres/groupements/inconnus, identité par IDs/métier, copie immuable/flags bruts/bornes, activation/propriétaire/disposal et absences/erreurs/publications tardives. Les 260 tests précédents passent. Aucune lecture de mémoire d'un processus en cours n'a été exécutée par ApiProbe : métadonnées uniquement.

## Essai en jeu différé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.40.0.0. Activer l'observation dans Craftimizer, ouvrir une synthèse d'essai ordinaire et comparer recette, étape native/UI, PS et valeurs affichées. Utiliser un effet puis vérifier nom et compteur copié. Fermer/masquer la fenêtre : le cadre doit disparaître. Se déconnecter ou désactiver : aucun cadre ne doit réapparaître. Vérifier le bouton de contexte sur une recette identifiée, puis désactiver l'observation. Si un champ est inconnu, conserver son texte brut et son origine avant toute correction. Aucune réussite de ce protocole n'est déclarée ; test Macro Chain toujours reporté par l'utilisateur.

Prochaine étape : établir les correspondances d'effets/tours, l'historique des actions uniques et la convention des étapes. Une conversion vers le moteur ne sera permise qu'avec la couverture nécessaire ; aucune valeur absente ne doit devenir zéro.
