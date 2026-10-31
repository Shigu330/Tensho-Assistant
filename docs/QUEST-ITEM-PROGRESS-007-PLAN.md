# QUEST-ITEM-PROGRESS-007 — Prochaine tranche préparée

2026-10-29. READY pour étude/implémentation à la reprise ; aucune capacité de progression ajoutée en 0.70.0 par ce document.

## Besoin

La liste « Quêtes proposant cet objet » filtre nom/catégorie/ID et conserve les variantes, mais n’affiche pas la progression déjà observée dans le suivi des quêtes. Relier cette liste au snapshot courant permettrait de distinguer les quêtes en cours et terminées sans prétendre qu’une autre quête est disponible.

## État inspecté

- Quests/QuestService.cs : QuestEntry.Progress possède Unconfirmed, Active et Complete ; Sequence et IsRepeatable sont déjà copiés. QuestState fournit CharacterId, ObservedAt, Entries et l’index de récompenses. Publish hors ligne force Unconfirmed et Sequence=null. Invalidate retire propriétaire/date/index et remplace les états par Unconfirmed, avec Loading=true ; Clear retire aussi les entrées. Ces chemins ont été relus pour ce plan. Relire le reader avant implementation pour confirmer la couverture de l’observation.
- QuestRewardIndex reste volontairement statique ; Search ne connaît pas de personnage. Ne pas intégrer la progression dans ses associations ou lui ajouter un lecteur.
- MainWindow.Quests.cs affiche déjà les trois états et la séquence. MainWindow.QuestItemSources.cs cache par référence d’index, objet et requête ; ce cache seul ne suffirait pas à invalider une nouvelle projection de progression.
- MainWindow.ItemUses.cs appelle les sources de quête avant le retour anticipé du catalogue des autres usages. Préserver cet ordre et la navigation SelectQuest existante.

## Contrat proposé

Construire une projection managée des candidats de l’objet et du même snapshot QuestState, jointe par QuestId complet. Afficher état observé, séquence éventuelle et date/portée. Hors ligne, ligne absente, snapshot en erreur/en lecture ou observation invalidée : progression non confirmée, jamais « non commencée », « disponible » ou « récompense récupérable ».

Les filtres éventuels restent explicitement « Toutes / En cours confirmées / Terminées confirmées / Non confirmées ». Ne pas masquer par défaut les quêtes terminées : IsRepeatable ne prouve pas leur réinitialisation actuelle. Ne pas sommer les récompenses alternatives ou déduire un rendement récupérable de la progression.

Réutiliser les snapshots existants ; aucune requête native ou réseau par ligne ou par frame. Cache de jointure/projection lié au snapshot pertinent, à l’objet, au filtre et à la requête, invalidé lors du changement de personnage, logout et nouvelles observations. Conserver les catégories, tooltips, clipper, variantes et liens actuels.

## Vérification à réaliser

Tests de jointure sur IDs complets, ligne absente et owner hors ligne ; même personnage avec nouveau snapshot, changement de personnage, invalidation/clear et réponses tardives. Une quête répétable terminée reste terminée confirmée sans affirmation de réouverture. Variantes identiques avant/après filtre et erreur de progression distincte d’une absence de source. Vérifier le chemin actuel dans le code avant de choisir les nouvelles classes ; éviter un second index natif ou une duplication du service.

Exécuter scripts/Verify.ps1 après modification de code, contrôler la version DLL/manifeste/ZIP, mettre à jour les MD et créer un nouveau checkpoint immuable. Aucun nouvel essai projection craft/Macro Chain à demander. Les 17 domaines restent PARTIAL.

## Point de livraison conservé

0.70.0.0, 435/435 tests et paquet verts, zéro avertissement/erreur. DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Source : checkpoints/TenshoAssistant-0.70.0-source.zip. Ce plan ultérieur sera conservé séparément, sans modifier ce ZIP.
