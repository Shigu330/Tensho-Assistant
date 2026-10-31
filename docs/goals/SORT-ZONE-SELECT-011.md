# SORT-ZONE-SELECT-011 — Sélection multiple dans les zones de tri

0.77.0.0 — 2026-10-30 — IMPLEMENTED / COMPILED / TESTED (Verify.ps1 exécuté par l'agent sur le PC, 19:2x : 481/481, 0 avertissement/erreur, langues et paquet verts). VALIDATED_IN_GAME: YES (0.77–0.80, DLL 0.81.0.0) — captures et rapport utilisateur 2026-10-30 (« tous est ok ») ; non observé directement par l'agent.

Demande utilisateur du 2026-10-30, après validation en jeu du tri natif : une zone ne pouvait viser qu'une catégorie et un seul texte ; cliquer un objet de l'aperçu devait permettre de le sélectionner.

## Comportement

- **Catégories multiples** : chaque zone accepte jusqu'à 64 catégories (union). Ajout par liste déroulante, retrait par bouton « × ». Le champ historique `CategoryId` reste lu comme membre de l'union ; toute modification le replie dans `CategoryIds`.
- **Objets choisis** : jusqu'à 128 identifiants exacts par zone (`ItemIds`), affichés avec leur nom.
- **Termes de recherche** : le texte est découpé sur « ; » (espaces et termes vides ignorés). Un objet correspond si son nom contient un terme (sans casse) ou si son ID contient un terme.
- **Sélection** = objets choisis ∪ termes ; sans liste ni terme, tous les objets. Elle reste combinée (ET) avec catégories, qualité, vendable, HQ possible et emplacement.
- **Clic sur l'aperçu** : un clic sur une pile ouvre un menu par zone filtrée : ajouter cet objet, ajouter sa catégorie, ajouter son nom aux termes ; ou créer une zone d'une case pour cet objet. Les actions modifient seulement le brouillon : rien n'est enregistré ni appliqué depuis ce menu.
- Les zones protégées n'apparaissent pas dans le menu (leurs filtres sont inactifs).

## Priorité des catégories (0.78.0.0)

Retour en jeu de l'utilisateur sur 0.77 (2026-10-30, capture : zone Médicament / Appât de pêche / Matéria affichée et fonctionnelle) : besoin de prioriser les catégories. L'ordre de la liste est désormais la priorité de la zone : piles de la catégorie 1, puis 2, etc. ; tri stable, donc les critères ordonnent l'intérieur de chaque catégorie. La priorité décide aussi quelles piles entrent quand la zone déborde ; « Remplir depuis la dernière case » part de la dernière case. Interface : catégories numérotées, bouton « < » pour remonter. Le `CategoryId` historique garde le premier rang. Verify.ps1 (agent, 19:20) : 482/482, 0 avertissement/erreur, DLL 0.78.0.0.

## Cases restantes partagées (0.79.0.0)

Retour en jeu sur 0.78 (captures utilisateur 2026-10-30) : zone de 50 cases (1–50) avec 10 piles correspondantes ⇒ 40 cases réservées vides, le reste commençant en case 51 ; « Protéger les cases actuelles » figeait l'ordre actuel du jeu et désactivait les filtres (perçu comme « bizarre »). Ajout de l'option de zone `ShareUnused` (« Laisser les autres piles utiliser les cases restantes ») : après placement des piles de la zone, ses cases encore vides redeviennent libres et les autres piles les occupent dans l'ordre, sans trou. Incompatible avec une zone protégée. Défaut : faux pour les zones déjà enregistrées (comportement inchangé), vrai pour les zones créées depuis l'interface. Texte d'aide de la protection clarifié (EN/FR). Verify.ps1 (agent, 19:24) : 483/483, 0 avertissement/erreur, DLL 0.79.0.0.

## Recherche dans « Ajouter une catégorie » (0.80.0.0)

Demande utilisateur 2026-10-30 : pouvoir écrire pour trouver une catégorie. La liste déroulante commence par un champ de filtre (focus automatique, vidé à chaque ouverture), insensible à la casse et aux accents (« med » trouve « Médicament ») ; message si aucune catégorie ne correspond. Le dépôt n'active pas InvariantGlobalization ; le comportement des accents dépend toutefois de l'ICU de l'hôte, non observé en jeu. Verify.ps1 (agent, 19:29) : 483/483, 0 avertissement/erreur, DLL 0.80.0.0 ; rendu non couvert par les tests.

## Compatibilité et limites

Les deux listes sont des paramètres optionnels (null par défaut) : les profils et modèles enregistrés avant 0.77 se chargent inchangés (test de désérialisation v1 en System.Text.Json ; la configuration Dalamud passe par Newtonsoft, non testée séparément). Validation : identifiants non nuls, distincts, bornés. Copies profondes des listes dans les profils et modèles.

Hors périmètre : nouvelles propriétés d'objet (SORT-ITEM-PROPS-008), arsenal, automatismes, exclusions (« sauf »), opérateurs OU entre filtres différents.

## Tests

`InventorySortSelectionTests` (4 groupes) : union des catégories avec le champ historique et la qualité ; objets choisis + termes « ; » (union, ID, ET avec catégories, sélection vide) ; refus de zéros, doublons et dépassements ; sérialisation, format v1 et détachement des tableaux appelants (profil enregistré et modèle). Le menu de clic et le rendu ImGui ne sont pas couverts par les tests managés.

## Protocole en jeu (DLL 0.77.0.0)

1. Zone avec deux catégories ⇒ l'aperçu y place les piles des deux (fait en 0.77, capture utilisateur) ; en 0.78, « < » sur la 2ᵉ ⇒ ses piles passent devant.
2. Cliquer un objet de l'aperçu ⇒ « Ajouter cet objet » à une zone ⇒ il y apparaît ; retrait par « × ».
3. « Ajouter son nom à la recherche » ⇒ terme ajouté avec « ; ».
4. « Créer une zone pour cet objet » ⇒ nouvelle zone d'une case.
5. Enregistrer, changer de rangement puis revenir ⇒ listes conservées ; un ancien profil/modèle se charge toujours.
