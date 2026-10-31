# GATHER-002 — horaires statiques de récolte

État IMPLEMENTED / TESTED, version 0.20.0, 2026-10-28. Rendu Dalamud non validé en jeu.

Métadonnées locales vérifiées par ApiProbe : GatheringPointTransient possède GatheringRarePopTimeTable, EphemeralStartTime et EphemeralEndTime (UInt16). GatheringRarePopTimeTable possède des collections StartTime et Duration (UInt16). Les types seuls ne prouvent ni leurs unités ni la jointure.

Jointure vérifiée : GatheringPointTransient.RowId référence GatheringPoint.RowId, pas GatheringPointBase. Exemple réel 30999 → base 211 → table rare 1 : début 900, durée 300, soit 09:00–12:00 ET. Éphémère 31436 : 2000–0, soit 20:00–24:00 ET. Les durées natives distinctes sont 0, 160 et 300 ; 160 représente deux heures. Les débuts et bornes éphémères sont encodés HHmm. Les créneaux de durée nulle sont inutilisés, souvent avec début 65535. Deux bornes éphémères 65535 désignent l'absence de restriction horaire.

Source primaire CURRENT_VERIFIED le 2026-10-28 : [GatherBuddyReborn BitfieldUptime](https://github.com/FFXIV-CombatReborn/GatherBuddyReborn/blob/a204f2f45cb98302dbe938921255c3a6d617c775/GatherBuddy.GameData/Time/BitfieldUptime.cs) et [Node.Base](https://github.com/FFXIV-CombatReborn/GatherBuddyReborn/blob/a204f2f45cb98302dbe938921255c3a6d617c775/GatherBuddy.GameData/Classes/Node.Base.cs). Inspection des unités et jointures ; aucune implémentation copiée. Le modèle original utilise des intervalles en minutes, normalisés et immuables.

Implémentation : copie des valeurs sur le framework, horaires attachés au catalogue partagé, clé de déduplication conservant les fenêtres différentes d'une même base. Les objets cachés sont aussi préservés par le chemin ordinaire. Affichage et copie des horaires depuis les lieux de récolte. Pêche, points de type 8, transitoires ou tables manquants restent inconnus ; une table vide et des données invalides ont des états distincts. Sans restriction horaire ne signifie ni présent, ni débloqué, ni météo favorable. Aucun calcul de disponibilité actuelle.

Preuves : Verify.ps1 Release, zéro avertissement/erreur, 123/123 tests et package cohérent. Quatre groupes supplémentaires : encodage HHmm/160, chevauchements et minuit, inconnus/sentinelles/invalides, immutabilité et déduplication. GatheringProbe réutilise la conversion de production sur le sqpack installé : 1 221 sources sans restriction horaire, 534 limitées, 2 444 inconnues, aucune invalide. Assertions sur 30999 et 31436 passantes. Aucun pointeur ni ligne native différée vers worker.

Essai ultérieur : ouvrir GatherBuddy, sélectionner une ressource à point rare puis éphémère ; contrôler les fenêtres ET, la copie et une ressource de pêche indiquée inconnue. Vérifier le rendu sur fenêtre étroite. Cet essai n'a pas été demandé à nouveau pendant le report de Macro Chain.
