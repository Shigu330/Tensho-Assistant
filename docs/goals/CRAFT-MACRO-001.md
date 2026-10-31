# CRAFT-MACRO-001 — export manuel d'une séquence simulée

Version 0.37.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED. VALIDATED_IN_GAME : non. Extension de [CRAFT-SIM-001](CRAFT-SIM-001.md) ; Craftimizer reste PARTIAL.

## Fonction disponible

Depuis une simulation terminée sans erreur, ouvrir **Macros · copie manuelle**. Choisir éventuellement 0 à 3 secondes supplémentaires par attente et le message sonore de fin de bloc, puis Préparer le texte des macros. Chaque bloc peut être inspecté et copié séparément. Les noms sont ceux du métier exact dans les feuilles natives de la langue du client.

Une macro contient au plus 15 lignes. Avec message de fin, réserver une ligne et placer jusqu'à 14 actions ; sans message, jusqu'à 15. Les commandes sont `/ac "Nom natif" <wait.N>` ; le message optionnel est `/echo Tensho numéro/total <se.1>`. Aucun titre, commentaire ou séparateur n'est inclus dans le texte copié.

L'export inclut seulement les actions réellement simulées jusqu'à la fin de la synthèse ; le nombre d'actions planifiées ensuite et omises est visible. Une séquence incomplète, cassée ou invalide n'est pas exportable. Le détail des ressources et les limites du modèle restent ceux de la simulation. Modifier attributs/ordre/options ou recalculer invalide le texte préparé précédent.

Attentes proposées : 2 secondes pour les six effets Veneration, Innovation, Great Strides, Waste Not I/II et Manipulation ; 3 pour les autres actions prises en charge, y compris Observe et Master's Mend. Le réglage additionnel est appliqué à chaque action. La somme des attentes est affichée ; elle n'est ni une mesure de durée réelle ni une garantie d'activation. Latence, changement d'attributs et conditions réelles peuvent modifier le résultat.

Copier manuellement les blocs dans les macros du jeu, puis les lancer soi-même dans l'ordre après un essai en synthèse d'essai avec les mêmes attributs. Pas d'écriture du module natif, pas d'activation en jeu, pas de marqueur /tenshonext ajouté. Le test de Macro Chain reste différé conformément à la réponse utilisateur.

## Modèle et validation

CraftMacroExporter est pur : entrée simulation terminée + catalogue complet + métier + options ; sortie pages/lignes immuables. Limite 100 étapes, options bornées, noms non vides de longueur bornée sans guillemets, contrôles ou séparateurs de lignes. Aucune commande arbitraire issue d'un nom n'est concaténée hors guillemets. Les entiers d'attente emploient la culture invariante. Pas de service, abonnement, réseau ou tâche de fond supplémentaire. L'UI n'affiche un export que pour l'instance de résultat qui l'a produit.

CURRENT_VERIFIED le 2026-10-28 : [Craftimizer MacroCopy](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Craftimizer/Utils/MacroCopy.cs) pour la capacité de 15 lignes et les commandes localisées ; [BaseAction](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Actions/BaseAction.cs), BaseBuffAction, Observe et MastersMend pour les attentes 3/2 secondes et exceptions. Ces attentes sont une convention pratique de génération, pas une preuve du délai exact de chaque animation. Limite native de 15 lignes également utilisée par le lecteur de macros existant et les structures locales. Implémentation originale ; aucune écriture native reprise.

COMPILED / TESTED : Verify.ps1 Release 0.37.0.0, 243/243 tests, package/versions/assemblies hôtes vérifiés, zéro avertissement/erreur. Quatre nouveaux groupes couvrent les frontières 1/14/15/28/29/100 actions avec/sans message, conservation de l'ordre, noms localisés, attentes et surplus omis, rejet des simulations invalides et noms injectés, immutabilité et culture.

LOCAL_VERIFIED : CraftSimulationProbe utilise les 136 métadonnées du jeu ; export de la séquence de test vers chacun des huit métiers, noms natifs présents, préfixe simulé conservé et capacité respectée. Le scénario reste explicitement fictif : aucune activation en jeu observée.

Essai différé : DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.37.0.0. Simuler une recette ordinaire, préparer les macros, vérifier le nombre de lignes, copier chaque bloc dans une macro vide et essayer en synthèse d'essai. Contrôler les noms et animations ; ajouter une seconde si nécessaire. Vérifier qu'une modification de séquence retire le texte préparé et qu'un scénario incomplet ne peut pas être exporté. Cette vérification n'est pas déclarée réalisée.
