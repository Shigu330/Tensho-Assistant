# CACT-LIVE-002 — 0.48.0, 2026-10-29

Mini Cactpot propose un mode « Observer le ticket du jeu et actualiser les conseils », désactivé au démarrage et non persisté. Ouvrir le ticket dans le jeu puis la page de l’outil suffit : les chiffres observés remplacent la saisie et le conseil est recalculé à chaque révélation. Avec quatre chiffres, les huit lignes sont comparées. Aucune révélation, sélection de ligne, dépense ou commande n’est exécutée dans le jeu. La saisie manuelle reste accessible en désactivant le mode observé ; changer de mode efface le ticket de calcul.

## Capture et validité

Le lecteur propriétaire de son abonnement framework lit LotteryDaily uniquement lorsque son addon est prêt et visible, et l’UI du jeu affichée. Cadence maximale cinq lectures par seconde, copie bornée des neuf entiers GameNumbers en ordre ligne/colonne. Fenêtre, propriétaire et chiffres sont revérifiés avant publication. Aucun pointeur, span natif ou ligne Excel n’est conservé dans le cadre, l’UI ou un worker. Les erreurs sont signalées avec reprise espacée de cinq secondes.

Le cadre managé copie les neuf valeurs, exige 0–9 et aucun doublon parmi les chiffres non nuls. Zéro conserve le sens de case cachée. Seuls un à quatre chiffres rendent le plateau jouable. Zéro chiffre attend le ticket ; plus de quatre indique un ticket terminé ou une transition, sans conserver de conseil. Une observation de plus de deux secondes ou datée dans le futur est inutilisable. Fermeture, masquage, erreur, désactivation et changement de personnage retirent les données et révoquent les leases précédents.

La sémantique de GameNumbers est établie dans la structure nommée et le contrôleur historique, mais sa capture actuelle en jeu n’a pas été observée. Les refus évitent de transformer les neuf chiffres d’une fin de ticket en informations utilisables pendant la partie. Ils ne remplacent pas la validation runtime.

## Solveur et durée de vie

Le solveur mathématique existant est conservé. MiniCactpotSolveSession remplace le worker privé de la fenêtre : entrée copiée/readonly, propriétaire et révision, CTS et génération. Un résultat ou une erreur tardifs ne peuvent remplacer le ticket courant, même si l’ancien calcul ignore l’annulation. Les deux modes utilisent cette session ; suppression/édition, nouveau ticket, propriétaire différent ou Dispose invalident les travaux. Le lecteur est nettoyé via RegistrationScope et la session via la fenêtre. Pas de lecture native ni calcul exhaustif dans Draw : seule la synchronisation d’un cadre managé puis le lancement d’un worker y ont lieu.

Si la page n’est pas affichée, aucun nouveau calcul n’est lancé. À son retour, le contexte est synchronisé avant d’afficher un conseil ; un ancien résultat est invalidé. L’espérance affichée reste une espérance sur les chiffres cachés et les futurs choix optimaux, pas le gain garanti du ticket.

## Sources et preuves exécutées

LOCAL_VERIFIED : ApiProbe sur l’assembly installée confirme AddonLotteryDaily.GameNumbers, GameBoardNumbers et ses trois GameNumberRow, chacun avec Col1/2/3 et indexeur. Aucun getter natif exécuté dans le probe. [FFXIVClientStructs AddonLotteryDaily](https://github.com/aers/FFXIVClientStructs/blob/385b0821bc4573b77fbde84c930774eeca8283ec/FFXIVClientStructs/FFXIV/Client/UI/AddonLotteryDaily.cs), nom d’addon et indexeurs nommés vérifiés dans la source primaire.

HISTORICAL_VERIFIED : dépôt primaire archivé [MidoriKami/EzMiniCactpot, LotteryDailyController](https://github.com/MidoriKami/EzMiniCactpot/blob/63173afb3eff69aa64a3edc613a377c4053d26fa/MiniCactpotSolver/LotteryDailyController.cs), lecture GameNumbers et fin de ticket ; GameGrid pour l’ordre ligne/colonne. L’ancienne URL daemitus/ezMiniCactpot retourne 404 ; le dépôt trouvé chez MidoriKami est une référence historique. Aucun fichier LICENSE trouvé dans cet arbre : aucun code de ce contrôleur, solveur, animation ou nœud natif importé. Implémentation originale sur les types de la dépendance existante.

COMPILED / TESTED : Verify.ps1 Release 0.48.0.0, 311/311 tests, package/versions/ZIP/exclusion des assemblies hôtes passants, zéro avertissement/erreur. Neuf groupes nouveaux : copie et bornes, activation/propriétaire, désactivation/lifetime, absence/erreur/malformation, âge, solveur réel sur copie, succès tardif contre nouveau ticket, erreurs tardives après annulation/propriétaire/Dispose et erreur de saisie. Le solveur exact précédent conserve ses tests de référence.

Premier build de cette tranche refusé sur d’anciens champs du worker encore référencés dans Dispose et Diagnostics ; migration corrigée puis vérification complète passée. Aucun échec runtime déclaré résolu par compilation seule.

VALIDATED_IN_GAME : non. Protocole conservé dans TESTING.md : comparer chiffres et ordre après chaque révélation, phase ligne après quatre chiffres, puis retrait à fermeture/masquage et changement de ticket. Aucun nouveau test projection ou Macro Chain demandé ; leurs reports explicites sont conservés.

DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Les 17 domaines restent PARTIAL. Objectif global actif ; suite GATHER-NATIVE-004, étude de copie du point de récolte pour l’outil PR sans inventer des valeurs de base à partir des taux modifiés.
