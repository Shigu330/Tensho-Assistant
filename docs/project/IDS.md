# Identifiers of the project documents

> *Traduction française plus bas.*

One registry of the identifier families used in the documents of `docs/project/`. Since 2026-11-11 (audit of 2026-11-09, finding
A046), only two families still take new numbers; every other family is frozen: its identifiers stay as they are (the CHANGELOG and
the older documents cite them) and no new one is created.

## Families that take new numbers

| Family | Where | Rule |
|---|---|---|
| `X-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md) | any new request, bug or topic of the plan (rule "note before doing"); the next free number, added with the plan tool in both languages; a number that exists already is refused |
| `DEC-NNN` | [DECISIONS](DECISIONS.md) | a lasting decision; the next number after the last, written in both halves (the index generator refuses a gap) |

## Frozen families

| Family | Where | Meaning |
|---|---|---|
| `R-01` to `R-14` | [ROADMAP](ROADMAP.md), [PROJECT](PROJECT.md) | the user's first requests (R-14: resistance to game updates) |
| `D-NN` | [ROADMAP](ROADMAP.md) | decisions postponed to the end; settled ones point to their DEC |
| `A-`, `R-`, `I-`, `C-`, `M-`, `N-`, `X-` (01 to 15) | [GAPS](GAPS.md) | gaps by domain: A crafting, R gathering, I items, C collections, M map and character, N domains without a module, X cross-check additions |
| `C0` to `C36` (with `K` and `G`) | [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) | crafting and gathering proposals |
| `I0` to `I22` | [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md) | items, bank, sorting and market proposals |
| `P0` to `P27` | [MAP-PROPOSALS](MAP-PROPOSALS.md) | map proposals |
| `T0` to `T29` | [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md) | alarms, free company, quests, gear and mini-game proposals |
| `W-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md) | topics added on 2026-11-06 |
| `H1` to `H10`, parsers `P1` to `P10`, `E1` to `E5` | [GLOBAL-PLAN](GLOBAL-PLAN.md) (full cleanup, X-76) | harmonisation steps, parsers, extra steps |
| `D1` to `D5` | COMPETITIVE-OFF-PLAN (switch-off in PvP and ranked content) | questions of that plan, answered by DEC-038 |
| `REF-…`, `<MODULE>-NNN` (`UI-`, `ONBOARD-`…) | [REFERENCES](REFERENCES.md), [TESTING](TESTING.md) | verified sources; in-game test protocols |

## Known collisions

The same identifier names two things in these cases; cite it with its document's name:

| Identifier | Meanings | How to cite |
|---|---|---|
| `R-01` to `R-14` | user request (ROADMAP) and gathering gap (GAPS) | "R-14" for the request, "GAPS R-14" for the gap |
| `X-01` to `X-14` | plan point (GLOBAL-PLAN) and cross-check gap (GAPS) | "X-03" for the plan point, "GAPS X-03" for the gap |
| `C-01` to `C-15`, `C0` to `C36` | collection gap (GAPS) and crafting proposal | "GAPS C-05", "C5" |
| `P1` to `P10` | map proposal and X-76 parser | "P1" for the map, "parser P1" for the parser |
| `D-NN`, `D1` to `D5` | postponed decision (ROADMAP) and question of the switch-off plan | "D-83", "switch-off D1" |

---

# Identifiants des documents du projet

Un seul registre des familles d'identifiants des documents de `docs/project/`. Depuis le 2026-11-11 (audit du 2026-11-09, constat
A046), seules deux familles prennent encore de nouveaux numéros ; toutes les autres sont figées : leurs identifiants restent tels
quels (le CHANGELOG et les anciens documents les citent) et aucun nouveau n'est créé.

## Familles qui prennent de nouveaux numéros

| Famille | Où | Règle |
|---|---|---|
| `X-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md) | toute nouvelle demande, tout bug ou sujet du plan (règle « noter avant de faire ») ; le numéro libre suivant, ajouté par l'outil du plan dans les deux langues ; un numéro qui existe déjà est refusé |
| `DEC-NNN` | [DECISIONS](DECISIONS.md) | une décision durable ; le numéro qui suit le dernier, écrit dans les deux moitiés (le générateur de l'index refuse un écart) |

## Familles figées

| Famille | Où | Sens |
|---|---|---|
| `R-01` à `R-14` | [ROADMAP](ROADMAP.md), [PROJECT](PROJECT.md) | les premières demandes de l'utilisateur (R-14 : résistance aux mises à jour du jeu) |
| `D-NN` | [ROADMAP](ROADMAP.md) | décisions reportées à la fin ; celles tranchées renvoient à leur DEC |
| `A-`, `R-`, `I-`, `C-`, `M-`, `N-`, `X-` (01 à 15) | [GAPS](GAPS.md) | manques par domaine : A artisanat, R récolte, I objets, C collections, M carte et personnage, N domaines sans module, X ajouts du recoupement |
| `C0` à `C36` (avec `K` et `G`) | [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) | propositions d'artisanat et de récolte |
| `I0` à `I22` | [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md) | propositions objets, banque, tri et marché |
| `P0` à `P27` | [MAP-PROPOSALS](MAP-PROPOSALS.md) | propositions de la carte |
| `T0` à `T29` | [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md) | propositions alarmes, compagnie libre, quêtes, équipement et mini-jeux |
| `W-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md) | sujets ajoutés le 2026-11-06 |
| `H1` à `H10`, parseurs `P1` à `P10`, `E1` à `E5` | [GLOBAL-PLAN](GLOBAL-PLAN.md) (nettoyage complet, X-76) | étapes d'harmonisation, parseurs, étapes ajoutées |
| `D1` à `D5` | COMPETITIVE-OFF-PLAN (coupure en JcJ et en contenu classé) | questions de ce plan, tranchées par DEC-038 |
| `REF-…`, `<MODULE>-NNN` (`UI-`, `ONBOARD-`…) | [REFERENCES](REFERENCES.md), [TESTING](TESTING.md) | sources vérifiées ; protocoles d'essai en jeu |

## Collisions connues

Le même identifiant nomme deux choses dans ces cas ; le citer avec le nom de son document :

| Identifiant | Sens | Comment le citer |
|---|---|---|
| `R-01` à `R-14` | demande de l'utilisateur (ROADMAP) et manque de récolte (GAPS) | « R-14 » pour la demande, « GAPS R-14 » pour le manque |
| `X-01` à `X-14` | point du plan (GLOBAL-PLAN) et manque du recoupement (GAPS) | « X-03 » pour le point du plan, « GAPS X-03 » pour le manque |
| `C-01` à `C-15`, `C0` à `C36` | manque des collections (GAPS) et proposition d'artisanat | « GAPS C-05 », « C5 » |
| `P1` à `P10` | proposition de la carte et parseur de X-76 | « P1 » pour la carte, « parseur P1 » pour le parseur |
| `D-NN`, `D1` à `D5` | décision reportée (ROADMAP) et question du plan de coupure | « D-83 », « coupure D1 » |
