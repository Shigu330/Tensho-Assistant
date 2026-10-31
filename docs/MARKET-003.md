# MARKET-003 — coût des besoins terminaux

État IMPLEMENTED / TESTED, version 0.28.0, 2026-10-28. Rendu en jeu non observé.

La chaîne calculée propose une rubrique repliable Coût des besoins terminaux. Les quantités viennent du résultat de CraftChain, après consommation des stocks et surplus : aucune deuxième soustraction. Même modèle de coût, cache, cadence, qualités, tableau, copie et consultation de liste que les ingrédients directs. Aucun nouveau service réseau.

Les variantes ambiguës, cycles, limites de profondeur et ingrédients manquants restent des branches non résolues. Elles ne sont ni chiffrées, ni demandées au service marché. Si plusieurs raisons concernent le même objet, elles sont regroupées et toute raison non résolue conserve cet état. Le sous-total des besoins identifiés n'est pas présenté comme un total complet.

Une observation absente dans la chaîne ou son registre laisse les quantités terminales à vérifier ; les stocks incomplets et caches en amont sont propagés à l'estimation. Si aucun besoin terminal ne reste, zéro achat est affichable selon les observations du calcul, avec leurs limites conservées. Une chaîne sans besoin et sans aucune observation ne prouve pas zéro et est refusée. Montants et sommes restent contrôlés contre les dépassements.

Provenance : date UTC de la chaîne et dates des prix distinctes. Des stocks renouvelés depuis le calcul appellent un recalcul ; la copie conserve ce signal. Modifier les choix de recettes, recalculer ou effacer la chaîne annule sa consultation en cours. Le mode quantité des ingrédients directs n'altère pas les quantités terminales. La liste visible utilise un clipper pour les besoins nombreux ; la limite de consultation groupée de 32 objets est explicite.

COMPILED / TESTED : Verify.ps1 Release sans avertissement/erreur, 170/170 tests et package cohérent. Six groupes supplémentaires couvrent stocks intermédiaires/bruts sans double déduction, quatre types de branches non résolues et absence de consultation, observations en amont manquantes/incertaines/importées, zéro avec provenance et refus sans preuve, raisons mixtes/overflow, invalidation par révision de choix avec réponse tardive rejetée. Les 164 tests antérieurs restent passants.

LOCAL_DATA_VERIFIED : GatheringProbe sur la recette native Sirop de bouleau, cible sept objets → neuf produits. Avec stock SIMULÉ de trois sèves et prix SIMULÉS sève=10 / éclat de feu=1 : six sèves et vingt-quatre éclats restent, coût fixture 84. Ce contrôle ne lit ni un stock de personnage réel ni un prix du marché actuel. Le coût ne devient pas 54 par deuxième soustraction des trois sèves. Probe complet passant.

Essai ultérieur : calculer une chaîne, ouvrir son coût, comparer les quantités terminales au tableau des besoins et consulter la liste. Choisir une variante puis recalculer, vérifier annulation et provenance ; tester un besoin inconnu et une chaîne entièrement couverte. Pas de nouvelle demande maintenant ; Macro Chain reste reporté.

Suite : approfondir QuestTracker avec les prérequis et liens natifs, en vérifiant les jointures et en conservant les négatifs non confirmés inconnus. Les règles non couvertes, l'observation Triple Triade et le rendu des nouvelles fonctions restent à valider selon leurs périmètres.
