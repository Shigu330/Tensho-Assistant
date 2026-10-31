# CRAFT-LIVE-002 — identification des effets observés

Version 0.41.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. L'observation reste expérimentale et ne donne aucun conseil natif.

Extension 0.45 : [CRAFT-LIVE-003](CRAFT-LIVE-003.md) compare un coup en condition Normale, avec intervalles de combos et admissibilité native ; aucune rotation complète ni état inconnu transformé en fait. Les limites et comptes 0.41 ci-dessous décrivent cette tranche historique.

## Fonction disponible

Dans Synthèse en cours, le panneau **Effets identifiés · compteurs de statut** affiche les noms, icônes et descriptions natifs des effets reconnus. Les degrés de Calme intérieur utilisent leur icône de pile ; les autres compteurs sont des tours. Les neuf lignes textuelles du menu et les statuts bruts restent séparément inspectables.

Le champ Param des statuts porte le compteur, conformément au lecteur de Craftimizer étudié. RemainingTime n'est pas utilisé pour calculer les tours. Les douze identités suivantes sont vérifiées dans les fichiers du jeu :

| ID | Nom français natif | Interprétation dans cette tranche |
|---|---|---|
| 251 | Calme intérieur | 1–10 degrés, durée indéfinie |
| 252 | Parcimonie | 1–4 tours |
| 2226 | Vénération | 1–4 tours |
| 254 | Grands progrès | 1–3 tours |
| 2189 | Innovation | 1–4 tours |
| 257 | Parcimonie pérenne | 1–8 tours |
| 2191 | Mémoire musculaire | 1–5 tours |
| 1164 | Manipulation | 1–8 tours |
| 3813 | Main suprême | Présence de la protection, pas de durée déduite |
| 2190 | Dernières vérifications | Compteur observé, effet hors moteur actuel |
| 2665 | Attention totale | Présence, effet hors moteur actuel |
| 3812 | Opportunité experte | Compteur observé, effet hors moteur actuel |

Le catalogue d'actions possède ces métadonnées natives, copiées sur le framework à la fin de son chargement. Une liste fournie mais incomplète/incohérente n'est pas publiée. Les anciens tests et usages purement manuels peuvent créer un catalogue sans ces métadonnées ; le décodeur signale alors leur absence.

## Couverture et inconnus

Le lecteur marque la liste de statuts complète uniquement si sa longueur est bornée et le nombre d'entrées effectivement parcourues correspond à StatusList.Length. Une lecture partielle ne devient jamais une preuve d'absence. Doublon de statut connu, compteur zéro/hors bornes, deux Parcimonies simultanées ou effet reconnu non modélisé sont explicitement signalés. Les autres statuts, tels que nourriture, restent bruts et ne sont pas inventés comme effets de craft. Le succès du décodage des compteurs reconnus n'est pas une preuve que toutes les influences possibles sont couvertes.

La présence unique de Main suprême prouve qu'elle a été utilisée. Son absence au premier tour UI observé, avec liste complète et sans drapeau NotFirstStep, prouve seulement l'état initial sans utilisation. Aux tours suivants, son absence ne permet pas de distinguer disponibilité et protection déjà consommée : état **inconnu**. Aucun historique fictif n'est reconstruit lorsque l'observation commence au milieu d'une synthèse.

StandardTouchProc, AdvancedTouchProc et ObservedProc sont affichés comme indications natives nommées depuis les bits documentés dans l'assembly local. Ils ne sont pas encore traduits automatiquement en réduction de PS ou en combo du simulateur. Le lecteur primaire de Craftimizer suit les actions pour reconstruire ses ActionStates ; cette tranche n'importe pas son hook et ne remplace pas cette preuve par une hypothèse sur les drapeaux.

La convention de la source étudiée est StepCount UI moins un pour le simulateur, mais les deux étapes restent distinctes à l'écran tant qu'une observation du client n'a pas confirmé la cohérence des champs utilisés. Conditions non normales, historique d'action unique et états incomplets interdisent toujours d'affirmer une conversion complète. Aucun état n'est injecté dans CraftSolver, aucune action exécutée.

## Preuves

CURRENT_VERIFIED le 2026-10-28 : [Craftimizer SynthHelper](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Craftimizer/Windows/SynthHelper.cs), GetEffectStack/HasEffect et GetCurrentState ; [SimulatorUtils](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Craftimizer/SimulatorUtils.cs), IDs d'effets et icônes de piles ; SynthesisValues pour la convention d'étape. Lecture des faits uniquement, implémentation originale, aucun hook/offset repris.

LOCAL_VERIFIED : ApiProbe StatusList.Length et champs de Status ; CraftSimulationProbe sur les fichiers installés : douze IDs/noms/descriptions/icônes présents, Calme intérieur MaxStacks=10, icônes des autres effets à pile non variable. Les 208 variantes d'actions, recettes, macros et scénarios précédents restent vérifiés. Aucune capture d'une synthèse en cours par le probe.

COMPILED / TESTED : Verify.ps1 Release 0.41.0.0, 270/270 tests et package/versions/ZIP/assemblies hôtes verts ; zéro avertissement/erreur. Cinq nouveaux groupes : Param indépendant de RemainingTime, doublons/bornes/conflits, couverture/métadonnées absentes, utilisation unique inconnue et effets non modélisés/indications de combo/immutabilité. Les 265 tests précédents restent verts.

## Essai différé et suite

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.41.0.0. Activer l'observation et comparer en synthèse d'essai les compteurs du menu à ceux du panneau identifié, notamment Innovation, Manipulation et Calme intérieur. Main suprême présente : utilisation oui ; une fois consommée : inconnue sans historique. Comparer aussi les indications natives après Basic/Standard/Observe et les deux étapes affichées. Aucun de ces essais n'est déclaré réalisé. Macro Chain reste reporté par réponse utilisateur.

La conversion native pour conseils reste une extension à établir ; cette vérification runtime est conservée sans bloquer les autres domaines. Prochain travail indépendant : météo et prévisions des zones, utiles à Mappy/GatherBuddy, avec données et calcul communs. Les 17 plugins restent PARTIAL.
