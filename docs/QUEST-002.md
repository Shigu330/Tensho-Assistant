# QUEST-002 — Liens et fiches de quêtes, 0.31.0

Complément 0.31.0 : bouton Suivre/Ne plus suivre dans la fiche, groupe repliable Quêtes suivies, liste compacte avec icônes/liens et retrait. Les choix sont persistants, partagés entre personnages, limités à 64 nouveaux suivis ; la progression reste celle de l'observation courante. Les IDs absents du catalogue sont conservés et retirables. Une erreur de sauvegarde restaure la liste précédente et affiche une erreur. Configuration v1 compatible : une configuration ancienne reçoit une liste vide. Verify.ps1 : Release 0.31.0.0, 177/177 tests/package verts. Test supplémentaire de migration/rechargement des IDs complets et préférences préexistantes.

Essai différé complémentaire : suivre une quête, recharger le plugin, retrouver le suivi ; passer d'un personnage à un autre, vérifier la progression réobservée ; retirer le suivi. La version courante de la DLL est 0.31.0.0. Aucune action utilisateur n'est nécessaire pour poursuivre les autres tranches.

Complément 0.30.1 : catalogue et fiche deviennent deux vues distinctes (boutons Catalogue / Fiche sélectionnée). Icônes natives cliquables dans la liste et les liens, icône dans la fiche, recherche par identifiant complet exact. Verify.ps1 : Release 0.30.1.0, 176/176 tests/package verts, zéro avertissement/erreur. Le visuel reste à vérifier en jeu.

Complément 0.30.0 : niveaux renseignés (les deux seuils distincts), zone et identifiant brut du donneur copiés dans la fiche. Le bouton Consulter la carte de la zone sélectionne la carte dans Mappy sans inventer la position du PNJ : centre initial 20,5/20,5 et coordonnées modifiables dans le navigateur existant. Il exige l'identité exacte carte/territoire du catalogue ; les correspondances absentes ou incohérentes sont indiquées et ne sont pas ouvertes. Aucun seuil de niveau n'est comparé au personnage ni transformé en éligibilité.

LOCAL_VERIFIED 0.30 : 5 373 lignes avec niveau ; 5 226/5 265 lieux non nuls ont une carte et un territoire concordants. Les 39 autres ne produisent pas de bouton vers une autre zone. COMPILED / TESTED : 0.30.0.0, 176/176 tests et package verts. Essai en jeu toujours différé ; le protocole ci-dessous utilise maintenant la DLL 0.30.0.0.

La fiche du suivi des quêtes présente les prérequis directs, les quêtes qui les référencent, la progression observée et le caractère répétable. Cliquer un nom ouvre sa fiche ; Retour conserve un parcours borné à 64 entrées. Les filtres du catalogue sont indépendants du parcours. La fiche et ses groupes sont repliables.

## Données et limites

`Quest.PreviousQuest` conserve les identifiants de ligne complets, jamais tronqués en ushort. Les zéros et doublons sont retirés ; un lien vers une ligne sans nom reste visible par son ID. Le graphe est un index direct dans les deux sens, sans parcours récursif ni problème de cycle. Les collections sont copiées et immuables. L'index UI est reconstruit seulement au changement d'observation ; aucune lecture native dans Draw.

`PreviousQuestJoin` : 1 = toutes, 2 = au moins une. Les modes inconnus restent explicitement non interprétés. Seules les fins confirmées comptent dans la preuve de complétion des liens. Une quête active, absente ou non confirmée ne devient jamais terminée. Une liste vide ne prouve aucune condition satisfaite. Cela ne constitue jamais un calcul de disponibilité : métier, niveau, faction, exclusions, horaires, événements et autres conditions restent hors de cette tranche.

`Unknown7` est copié comme valeur brute du premier lien. Une référence primaire l'utilise comme seuil de séquence ; les 160 valeurs non nulles du jeu installé sont signalées sans conclure que l'étape est satisfaite. Les fins confirmées ne prouvent pas une réinitialisation de quête répétable. Les états sont invalidés au changement de personnage, erreur ou déconnexion comme auparavant.

## Preuves 2026-10-28

- LOCAL_VERIFIED : métadonnées Lumina installées ; GatheringProbe lit 5 373 quêtes nommées, 5 213 liens directs ; modes 1 : 5 271, mode 2 : 102 ; 160 étapes brutes non nulles. Chaque lien inverse correspond à son lien direct ; aucune complétion déduite d'états non confirmés.
- CURRENT_VERIFIED : [énumération Questionable](https://github.com/PunishXIV/Questionable/blob/bb2bf7cfa0d3e410254f9a364271cbcbe856adb5/Questionable.Model/Common/EQuestJoin.cs), [adaptateur QuestInfo](https://github.com/PunishXIV/Questionable/blob/bb2bf7cfa0d3e410254f9a364271cbcbe856adb5/Questionable/Domain/QuestInfo.cs). Sémantique examinée ; aucune implémentation externe copiée.
- COMPILED / TESTED : Release 0.29.0.0, 175/175 tests et package vérifiés. Cinq nouveaux tests couvrent identités complètes, cycles, jointures, inconnus, copies et invalidation propriétaire.
- VALIDATED_IN_GAME : non. L'essai en jeu reste distinct, sans relancer la demande Macro Chain reportée par l'utilisateur.

## Essai différé

Recharger `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`. Accueil & outils → QuestTracker : sélectionner une quête, ouvrir ses prérequis puis une quête suivante, vérifier Retour et le repli de la fiche. Une quête terminée doit afficher la confirmation ; déconnecter/reconnecter ne doit pas conserver les confirmations du personnage précédent. Un lien absent reste identifiable. Aucune étiquette « disponible » ne doit apparaître.
