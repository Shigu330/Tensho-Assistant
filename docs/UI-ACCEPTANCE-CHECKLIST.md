# Validation de la refonte dans FFXIV

Validation en jeu confirmée par l’utilisateur le 2026-10-30 : « les verif en jeu
son OK ». Cette acceptation est rapportée par l’utilisateur ; l’agent n’a pas
effectué lui-même les essais ni mesuré les performances. La liste ci-dessous
reste le protocole de référence pour les prochains changements.

## Build à utiliser

DLL : `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll`.
Paquet : `src/TenshoAssistant/bin/x64/Release/TenshoAssistant/latest.zip`.
Version d’assemblage : **0.75.0**. Utiliser le build récent, pas une ancienne copie
Debug portant le même numéro. Le manifeste et la DLL du ZIP sont vérifiés par
leurs empreintes contre les fichiers Release canoniques.

Depuis la racine du dépôt : `./scripts/Verify.ps1`. Le script trouve le SDK local
10.0.403 ; un autre emplacement peut être fourni avec `-DotNet`.

## Contrôles à effectuer

1. Charger le plugin et ouvrir `/tensho`, puis le bouton de configuration de
   Dalamud. Le premier conserve le hub ; le second ouvre les paramètres.
   Vérifier l’absence d’erreur de chargement ou de pile ImGui dans les journaux.
2. Parcourir les catégories et leurs sous-outils, dont PR, Mage bleu, météo,
   butins, monstres, tri, Revue de mode et Triple Triade. La catégorie principale
   reste sélectionnée. Le répertoire complémentaire conserve ses 18 accès.
3. Tester les tailles 760 × 480, 1040 × 700 et une grande fenêtre. Vérifier le
   défilement, les textes longs, les listes, les fiches modales, la banque et le
   placement des panneaux compagnons près des bords de l’écran.
4. Passer de français à anglais, puis vérifier les langues partielles et leur
   fallback anglais. Contrôler les accents et les glyphes japonais avec les
   polices du jeu. Recharger le plugin : le choix explicite doit être conservé.
5. En mode développeur, modifier un libellé JSON externe puis utiliser
   **Recharger les traductions**. Vérifier aussi les libellés mis en cache et le
   mode d’affichage des clés. Un format invalide doit être diagnostiqué sans
   remplacer un texte embarqué valide par une chaîne cassée.
6. Activer et désactiver séparément les cinq préférences d’observation, puis
   recharger le plugin. Les choix restent indépendants. Une configuration
   ancienne sans ces préférences ne doit rien activer automatiquement.
7. Comparer l’horloge du hub à l’heure locale de Windows. Vérifier les dates des
   relevés, des séquences sauvegardées et des calculs. Les horaires d’Éorzéa
   restent distincts ; les comptes à rebours doivent continuer normalement.
8. Vérifier recherche d’objet, quantités NQ/HQ, ingrédients, variantes de
   recettes, préparation, simulateur, séquences et exports. Le menu contextuel
   d’inventaire doit réutiliser l’objet sélectionné sans lancer d’action.
9. Tester les imports manuels de rangements et leur remplacement après un
   transfert. Changer d’origine, de filtre et de personnage doit invalider la
   vue de banque. Le cache importé garde son caractère historique et sa date.
10. Vérifier les brouillons, modèles et zones de tri, notamment un chevauchement
    de zones et un modèle incompatible. Charger ou enregistrer une règle ne
    doit pas l’appliquer ; l’application reste un clic explicite distinct.
11. Vérifier les consultations communautaires : source, date de récupération,
    absence de date de mise à jour, cache et actualisation manuelle. Une liste
    de drops absente ne prouve pas l’absence de drops. Le wiki ne doit pas
    inventer une correspondance avec un identifiant natif de monstre.
12. Comparer CPU, allocations et fluidité entre hub, banque chargée, simulateur
    et observation. Vérifier fermeture, déconnexion et rechargement : aucune
    tâche tardive ne doit republier un résultat pour un ancien contexte.

Les fonctions expérimentales restent à valider explicitement. Cette liste ne
demande pas de démarrer une séquence de macros ni une fabrication automatique.

## Preuves à conserver

Noter version/empreinte du build, langue, taille de fenêtre et résultat de chaque
contrôle. Pour une anomalie : écran concerné, action effectuée, résultat attendu,
résultat observé et extrait pertinent du journal Dalamud. Ne marquer le rendu et
les performances **VALIDATED_IN_GAME** qu’après ces essais.
