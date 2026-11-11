# Content tools (X-90, X-92) — study of 2026-11-09

> *Traduction française plus bas.*

Request (the "to do" document): look at what the existing plugins do, then build suitable tools for Eureka (elemental level,
distribution of the magia), Bozja, deep dungeons, the Occult Crescent island (Île de Lunule), strategy boards for the content and the
fatal raids, and relics by version. Framework: DEC-041 (preparation only, nothing during combat, nothing that reads combat, PvE only,
switched off in PvP and in ranked content), Dalamud rules kept (DEC-039), X-92 (nothing hard-coded per content when the game provides it, designed per family).
The plugins quoted were looked at for their functions only (public pages), never their code.

## What Tensho already has

| Family | Already in Tensho | Source |
|---|---|---|
| Eureka, Bozja | FATEs of each zone, skirmishes, critical engagements, field records, lost actions (0.717, 0.721) | game sheets |
| Deep dungeons | the four dungeons, floors, items (apples, stones), loot (0.718); Tensho switches itself off inside them (DEC-038) | game sheets |
| Relics | all families, weapons and armour, stage reached according to your bags; view by version (0.974) | game sheets |
| S ranks | spawn conditions, places of the FATEs seen (0.670, 0.971) | community + discoveries |
| After an update | new entries in the sheets in the log and in the self-check (0.972) | game sheets |
| Field states | development record for Eureka, Bozja, the Occult Crescent (0.973); no page yet | game (your progress) |

## Existing plugins (public functions)

| Plugin | Does | Note for Tensho |
|---|---|---|
| Eureka Helper (snorux) | tracking of the NMs with a shared site, management of the elementals, relic help, alarms | the shared tracking goes through a third-party site: outside Tensho (network, Dalamud rule); relics already covered |
| NecroLens | in deep dungeons: nearby objects highlighted, floor timers | drawing what is hidden or automating: refused (Dalamud rules, DEC-038) |
| MonsterDex | bestiary and tips during deep dungeons | combat tips in game: outside DEC-041; a bestiary outside the dungeon would be preparation |
| Bozja, Occult Crescent | no plugin found; a tracking site for lost actions exists | free ground for Tensho |

## Proposals per family (preparation only)

| # | Tool | Data | Dalamud rule | Recommended default |
|---|---|---|---|---|
| 1 | Eureka: "Your progress" page: elemental level and experience, distribution of the magia, magicite, from the record of 0.973 | your state (game) | no (reading your state, outside combat) | to do after the in-game record |
| 2 | Bozja: lost actions owned and holstered, next to the existing list; field records obtained | your state (game) | no | to do after the record |
| 3 | Occult Crescent: knowledge, support jobs and their levels, silver and gold; FATEs and engagements of the island from the sheets (like Eureka) | game | no | to do after the record |
| 4 | Deep dungeons: bestiary by range of floors, outside the dungeon (preparation), from the sheets when they provide it | sheets; otherwise community | to confirm: tip texts | page outside the dungeon only, without tips until accepted |
| 5 | Strategy boards for the content and fatal raids: notes written by the player (placements, order), one per content, list of contents taken from the sheets (X-92), hidden in combat | your notes | yes: summary and agreement before coding | propose a notes format, with no drawing on the game screen |
| 6 | Families (X-92): one page template for the contents of a same family (fields, deep dungeons, raids), fed by ContentFinderCondition and ContentType | sheets | no | yes |

## Questions for the user (final batch)

1. Strategy boards (no. 5): simple notes written by you, hidden in combat, with nothing drawn on the game screen? (recommended)
2. Deep dungeons (no. 4): a bestiary page outside the dungeon, without combat tips? or with the community's tips, outside the dungeon only?
3. Shared tracking of the Eureka NMs through a third-party site: left to the existing plugins? (recommended: yes, Tensho stays without a third-party site)

Answers ([DEC-045](DECISIONS.md), 2026-11-09, point 8): 1. yes: written notes and an arena diagram in the Tensho window, out of combat,
never over the game, no marker placed in the game; 2. a bestiary with the wiki notes, outside the dungeon only. Question 3 was not in that
batch and stays open; Tensho has no third-party site.

---

# Outils de contenus (X-90, X-92) — étude du 2026-11-09

Demande (document « a faire ») : regarder ce que font les plugins existants, puis faire des outils adéquats pour Eurêka (niveau élémentaire,
répartition de la magia), Bozja, les donjons profonds, l'île Lunule (Croissant occulte), des tableaux stratégiques pour les contenus et les
raids fatals, les reliques par version. Cadre : DEC-041 (préparation seulement, rien pendant le combat, rien qui lise le combat, JcE seulement,
coupé en JcJ et en contenu classé), règles Dalamud gardées (DEC-039), X-92 (rien de figé par contenu quand le jeu le donne, pensé par famille).
Les plugins cités ont été regardés pour leurs fonctions seulement (pages publiques), jamais leur code.

## Ce que Tensho a déjà

| Famille | Déjà dans Tensho | Source |
|---|---|---|
| Eurêka, Bozja | ALÉA de chaque zone, escarmouches, affrontements cruciaux, rapports du front, actions oubliées (0.717, 0.721) | feuilles du jeu |
| Donjons profonds | les quatre donjons, étages, objets (pommes, pierres), butin (0.718) ; Tensho se coupe dedans (DEC-038) | feuilles du jeu |
| Reliques | toutes les familles, armes et armures, étape atteinte d'après vos sacs ; vue par version (0.974) | feuilles du jeu |
| Rangs S | conditions d'apparition, lieux des ALÉA vus (0.670, 0.971) | communauté + découvertes |
| Après une mise à jour | nouveautés des feuilles au journal et à l'auto-contrôle (0.972) | feuilles du jeu |
| États des champs | relevé de développement d'Eurêka, Bozja, Croissant occulte (0.973) ; pas encore de page | jeu (votre progression) |

## Plugins existants (fonctions publiques)

| Plugin | Fait | Remarque pour Tensho |
|---|---|---|
| Eureka Helper (snorux) | suivi des NM avec un site partagé, gestion des élémentaux, aide aux reliques, alarmes | le suivi partagé passe par un site tiers : hors de Tensho (réseau, règle Dalamud) ; reliques déjà couvertes |
| NecroLens | dans les donjons profonds : objets proches surlignés, minuteurs d'étage | dessiner ce qui est caché ou automatiser : refusé (règles Dalamud, DEC-038) |
| MonsterDex | bestiaire et conseils pendant les donjons profonds | conseils de combat en jeu : hors DEC-041 ; un bestiaire hors du donjon serait de la préparation |
| Bozja, Croissant occulte | aucun plugin trouvé ; un site de suivi des actions oubliées existe | place libre pour Tensho |

## Propositions par famille (préparation seulement)

| # | Outil | Données | Règle Dalamud | Défaut recommandé |
|---|---|---|---|---|
| 1 | Eurêka : page « Votre progression » : niveau et expérience élémentaire, répartition de la magia, magicite, d'après le relevé de 0.973 | votre état (jeu) | non (lecture de votre état, hors combat) | à faire après le relevé en jeu |
| 2 | Bozja : actions oubliées possédées et au holster, à côté de la liste existante ; rapports du front obtenus | votre état (jeu) | non | à faire après le relevé |
| 3 | Croissant occulte : savoir, métiers de soutien et leurs niveaux, argent et or ; ALÉA et affrontements de l'île depuis les feuilles (comme Eurêka) | jeu | non | à faire après le relevé |
| 4 | Donjons profonds : bestiaire par tranche d'étages, hors du donjon (préparation), depuis les feuilles quand elles le donnent | feuilles ; sinon communauté | à confirmer : textes de conseil | page hors donjon seulement, sans conseil tant que non accepté |
| 5 | Tableaux stratégiques des contenus et raids fatals : notes écrites par le joueur (placements, ordre), une par contenu, liste des contenus tirée des feuilles (X-92), masqués en combat | vos notes | oui : résumé et accord avant de coder | proposer un format de notes, sans dessin sur l'écran du jeu |
| 6 | Familles (X-92) : un même modèle de page pour les contenus d'une même famille (champs, donjons profonds, raids), alimenté par ContentFinderCondition et ContentType | feuilles | non | oui |

## Questions pour l'utilisateur (lot de fin)

1. Tableaux stratégiques (n° 5) : de simples notes écrites par vous, masquées en combat, sans rien dessiner sur l'écran du jeu ? (recommandé)
2. Donjons profonds (n° 4) : une page bestiaire hors du donjon, sans conseil de combat ? ou avec les conseils de la communauté, hors du donjon seulement ?
3. Suivi partagé des NM d'Eurêka par un site tiers : laissé aux plugins existants ? (recommandé : oui, Tensho reste sans site tiers)

Réponses ([DEC-045](DECISIONS.md), 2026-11-09, point 8) : 1. oui : notes écrites et schéma de l'arène dans la fenêtre de Tensho, hors
combat, jamais par-dessus le jeu, aucun marqueur posé dans le jeu ; 2. un bestiaire avec les notes du wiki, hors du donjon seulement. La
question 3 n'était pas dans ce lot et reste ouverte ; Tensho n'a aucun site tiers.
