# DUTY-001 — Butins des contenus, 0.32.0

Complément 0.33.0 : transport extrait vers Community/GarlandClient et partagé avec Monster Loot. DutyLootService possède son transport seulement en usage autonome ; le plugin fournit le transport commun et le dispose après les deux services. Cadence 3 s et bornes HTTP communes, mêmes caches/générations métier. Les tests de 0.32 restent verts dans les 206 tests de 0.33 ; le rendu en jeu reste non observé. La DLL courante est 0.33.0.0. Les paragraphes ci-dessous décrivent le checkpoint initial.

Implémenté et testé : Accueil & outils → Duty Loot Preview → Ouvrir les butins des contenus. Le catalogue est tiré des fichiers du jeu, la liste de butin vient d'une consultation explicite de Garland Tools. La sélection d'un contenu seule ne fait aucune requête.

## Parcours

1. Rechercher un contenu par son nom ou son identifiant du menu des missions, avec catégorie native facultative.
2. Cliquer son nom ou son icône pour ouvrir sa fiche, puis Consulter le butin. Le bouton Actualiser auprès de la source contourne seulement le cache, jamais la cadence.
3. Filtrer les objets par nom/ID et par récompenses, coffres de combat ou coffres de zone. Les sources d'un même objet sont regroupées ; leur détail reste en infobulle.
4. Cliquer une icône ou un nom d'objet connu pour l'envoyer au contexte commun, puis utiliser Stocks, Recettes ou Prix. La page de butin conserve sa sélection.
5. Annuler une lecture en cours ou choisir un autre contenu. Les résultats tardifs de l'ancienne sélection ne sont ni publiés ni ajoutés au cache.

## Sources et couverture

Le catalogue n'inclut que les InstanceContent nommés avec lien réciproque ContentFinderCondition.Content, ContentLinkType 1 et territoire non nul. Les identités mission, instance et territoire sont distinctes. 672 contenus passent les contrôles sur les données installées, dont 632 ont un ID de mission différent de l'ID d'instance. Cela ne garantit pas leur disponibilité actuelle ni la présence d'une table externe.

La requête GET vers Garland Tools contient l'ID d'instance ; aucun personnage, inventaire, historique ou monde n'est envoyé. La réponse est en anglais, mais les objets et contenus sont nommés depuis le catalogue français local. Le nom de source de combat, s'il existe, reste celui du fournisseur. Voir [étude et sources primaires épinglées](DUTY-001-STUDY.md). Implémentation originale sans inclusion de code/CSV/paquet tiers.

Récompenses, coffres de combat et coffres de zone sont séparés. Un item dans deux coffres conserve les deux provenances. Les partials ne sont pas traités comme des drops. Les drops directs de combat (`fights[].drops`), non interprétés, déclenchent une indication explicite s'ils sont présents. Aucun rapprochement par nom anglais vers un nom français n'est tenté. Les objets non connus du catalogue restent affichés par ID, sans sélection contextuelle incorrecte.

Une catégorie absente est distincte d'un tableau vide, et l'exhaustivité reste toujours non prouvée. Aucun taux de drop, quantité ou acquisition garantie n'est déduit. Une date de récupération ne représente pas la date de mise à jour de la source, qui demeure inconnue. Un échec réseau conserve éventuellement l'ancien document daté avec son message d'erreur ; il ne produit jamais une nouvelle liste vide de succès.

## Ownership et bornes

DutyCatalogLoader copie au plus 32 lignes par callback framework, avec budget souple 2 ms, puis construit l'index depuis les seules données managées. L'abonnement et le worker sont possédés, annulés/vidés au Dispose, erreurs explicites. Draw ne lit ni feuille ni pointeur natif.

DutyLootService possède son HttpClient par défaut et chaque CTS ; un client emprunté reste utilisable après son Dispose. Pas de blocage UI, génération de sélection, délai de 12 s, cadence minimale 3 s sur horloge monotone. Cache mémoire de 32 instances, réutilisation fraîche pendant 30 min. La consultation reste manuelle après expiration. Dispose rejette toute publication ultérieure. Données publiques indépendantes du personnage ; aucune progression personnelle n'est mise en cache dans ce service.

Réponse limitée à 1 Mio déclaré et effectivement lu par flux, UTF-8 strict, profondeur JSON 32, 4 096 sources par groupe et 16 384 entrées distinctes. Les longues listes UI sont filtrées seulement au changement de données/requête et dessinées avec clipper ; textures empruntées pour la frame via le fournisseur existant.

## Vérifications exécutées le 2026-10-28

- COMPILED / TESTED : Verify.ps1, Release 0.32.0.0, zéro avertissement/erreur, 192/192 tests et contrôles manifeste/version/ZIP/exclusion des assemblages hôte verts.
- Tests ajoutés : catalogue et clés distinctes, localité de la sélection, route HTTP, cache/expiration/cadence, changement de sélection, chevauchements, annulation manuelle/externe, réponse tardive, conservation d'erreur, mauvaise identité/JSON, limites déclarées et en flux, éviction, Dispose/client emprunté, couverture des drops directs non interprétés.
- LOCAL_VERIFIED : GatheringProbe, 672 liens natifs réciproques, dont 632 identités mission/instance différentes.
- CURRENT_VERIFIED : service réellement utilisé par le probe, Sastasha mission 4/instance 4 → 120 entrées source/objet, 68 objets distincts, 0 référence inconnue ; Praetorium mission 16/instance 86 → 1 entrée source/objet, 0 référence inconnue. Ce sont des réponses externes à cette date, sans garantie de couverture complète.
- VALIDATED_IN_GAME : non. Aucun menu natif de mission/journal modifié. Macro Chain reste reporté selon le choix de l'utilisateur.

Probe réseau explicite uniquement : `dotnet run --project tools/GatheringProbe -- <sqpack> --duty-loot=4` ou `--duty-loot=first-distinct`. Sans cette option, aucune requête de butin n'est lancée.

## Essai en jeu différé

DLL : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.32.0.0. Ouvrir l'outil, chercher Sastasha, sélectionner puis consulter, vérifier les icônes et les trois catégories. Sélectionner un objet et ouvrir Stocks/Recettes ; vérifier le contexte. Revenir aux butins, consulter puis changer de contenu pendant une lecture et annuler une autre lecture : aucune réponse étrangère ne doit remplacer le contenu sélectionné. Vérifier sur fenêtre étroite et après rechargement. Ce protocole est conservé pour plus tard et ne bloque pas le développement.
