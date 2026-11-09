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
