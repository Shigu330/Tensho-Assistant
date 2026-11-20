# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

Summary per version, in French then `### English` (English part required since 0.219.0); the English part is also the body of the
commits and of the release notes on GitHub. Evidence cited: docs/evidence/.
Each version was built and tested by `scripts/Verify.ps1`; in-game validation is stated when the user reported it.
This file keeps the last release and the versions after it; older ones are in [docs/changelog/](docs/changelog/)
(moved by `tools/changelog.py archive`, one file per hundred versions).

## 0.1606.0
- Plan global, X-662 : Relecture du bloc 0.1603 à 0.1605, mineurs. M1 : le total des servants ne repart plus du dernier solde du journal que si la première lecture des gils du personnage est aussi son dernier solde du journal (GilRecorder.Seed, décision à la première lecture du personnage ; les lectures des servants attendent jusque-là) : un dépôt ou un retrait fait pendant que Tensho ne regardait pas (jeu lancé sans Dalamud, plugin coupé) n'est plus compté comme une vente « Marché » ni comme un transfert d'un seul côté (R06) ; le rechargement du plugin servant ouvert (cas de X-658) reste compté. M2 : la ligne du journal du jeu (GetLogMessageDetail et l'analyse du nom de l'expéditeur) n'est plus lue qu'au message qui attend ses identifiants, après les contrôles simples (ChatSources.ResolveRead la lit au plus une fois), et rien n'est lu pendant la coupure compétitive (R03.2). M3 : la preuve 0.1604, son test et l'entrée 0.1604 du CHANGELOG reprennent des montants et des heures inventés de même forme, avec les mêmes relations (les soldes et heures réels de l'utilisateur restent hors du dépôt) ; la somme des transferts des servants de la preuve, fausse de 1 527, est corrigée. X-662.
- Pour les joueurs : journal des gils : un dépôt ou un retrait fait pendant que le plugin était coupé n'est plus compté comme une vente au marché.
- Tests : ReviewFindings1606Tests. Verify.ps1 (agent) : 3304/3304, 0 avertissement, 0 erreur ; DLL 0.1606.0.0. Non validé en jeu.
### English
- Review of the block 0.1603 to 0.1605, minor findings. M1: the retainers' total starts from the journal's last balance only when the character's first gil reading is also the character's last balance in the journal (GilRecorder.Seed, decided at the character's first reading; the retainers' readings wait until then): a deposit or a withdrawal made while Tensho was not watching (the game played without Dalamud, the plugin turned off) is no longer counted as a « Market » sale nor as a one-sided transfer (R06); a plugin reload with a retainer open (X-658's case) is still counted. M2: the game's log line (GetLogMessageDetail and the parsing of the sender's name) is read only for the message waiting for its identifiers, after the cheap checks (ChatSources.ResolveRead reads it at most once), and nothing is read while Tensho is cut for competitive content (R03.2). M3: the 0.1604 evidence, its test and the 0.1604 CHANGELOG entry use invented amounts and times of the same shape, with the same relations (the user's real balances and times stay out of the repository); the evidence's sum of the retainers' transfers, wrong by 1,527, is corrected. X-662.
- For players: Gil journal: a deposit or a withdrawal made while the plugin was off is no longer counted as a market sale.

## 0.1605.0
- Plan global, X-661 : Relecture du bloc 0.1599 à 0.1602, mineurs. M1 : un objet de livraison spéciale présent dans plusieurs lignes de SatisfactionSupply (Charbon 38821 en 42.1 et 45.6, 38822 en 43.1 et 45.7, même client, mêmes seuils) donnait une règle par ligne : le tableau des objets de collection le montrait deux fois à l'identique. Les règles identiques sont gardées une fois (CollectableDeliveries.Distinct) ; des lignes qui diffèrent (seuils, client) restent à part, comme le jeu les a. M2 (défaut d'outil, hors version) : plan.py add refuse désormais un premier champ qui tient la place d'un numéro (« X-NEW », « X-TBD », « X-? ») et propose « X-auto » ; la fiche de X-657 a retrouvé son titre et sa définition de « fait ». X-661.
- Pour les joueurs : tableau des objets de collection : l'objet d'une livraison spéciale n'apparaît plus qu'une fois au lieu de deux.
- Tests : CollectableDeliveryOnce1605Tests. Verify.ps1 (agent) : 3299/3299, 0 avertissement, 0 erreur ; DLL 0.1605.0.0. Non validé en jeu.
### English
- Review of the block 0.1599 to 0.1602, minor findings. M1: a custom delivery's collectable in several SatisfactionSupply rows (Coal 38821 in 42.1 and 45.6, 38822 in 43.1 and 45.7, same client, same thresholds) gave one rule per row: the collectables table showed it twice, identical. Identical rules are kept once (CollectableDeliveries.Distinct); rows that differ (thresholds, client) stay apart, as the game has them. M2 (a tool defect, outside the version): the agent's plan tool now refuses a first cell standing in for a number and offers the automatic number; X-657's card got its title and definition of done back. X-661.
- For players: Collectables table: a custom delivery's item is listed once instead of twice.
