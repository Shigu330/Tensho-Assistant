# Adding a language

> *Traduction française plus bas.*

1. Copy `src/TenshoAssistant/Localization/Languages/en.json`.
2. Rename it with the language code, for example `es.json`.
3. Update `_meta` and translate the values.
4. Never rename translation keys.
5. Run the localization validator from the repository root (requires the .NET SDK specified in `global.json`):

   ```powershell
   dotnet run --project tests/TenshoAssistant.Tests -c Release -- --validate-localization src/TenshoAssistant/Localization/Languages
   ```

   Missing translations are allowed and fall back to English. Invalid JSON, metadata, formats, placeholders, duplicate keys and unknown
   keys must be fixed. `.\scripts\Verify.ps1` runs the same validation, and also fails when a setting tooltip named in the code is missing
   in `en.json` or `fr.json`.

6. Submit a pull request towards `Devloppement`. The « Translation » issue form can be used first to say which language you take.

English is the reference language. Missing keys fall back to English, then to `[Key.Name]`.

<!-- docgen:languages-en -->
Current state (counted in the language files, version <!-- docgen:version -->0.1251<!-- /docgen:version -->): English and French are complete (7489 keys; French must stay complete); German (`de.json`) translates 30 keys and Japanese (`ja.json`) translates 30 keys, not reviewed by native speakers.
<!-- /docgen:languages-en -->

The completion of each language is shown in
Settings → Debug and in the contributor panel of Diagnostics (developer mode).

Language files are discovered automatically; no enum or C# window changes are needed.
The languages live only inside `TenshoAssistant.dll` (embedded resources, since 0.248); the package ships no language file. To test a
translation, create a `Localization/Languages` folder next to the installed DLL and put the JSON file there: external values override
bundled values. Use Settings → Advanced to enable Developer mode, then Settings → Debug → Reload translations to load edits from the
installed plugin's `Localization/Languages` directory. The contributor panel of Diagnostics (developer mode) can show translation keys.
Reloading is explicit; JSON is never parsed by the normal translation lookup during rendering.

Invalid external placeholder formats are ignored at reload: the bundled value is kept, or English is used for a partial added language.
The diagnostics still report the invalid editable file so it can be corrected. Valid reordered placeholders are supported, for example
`{1}` before `{0}`.

Use descriptive keys such as `ToolDirectory.Crafting.Name`. Do not use numbered labels. Keep FFXIV item, action and location names in the
official game catalog: these files translate the plugin interface, and game data is shown in the game's own language.

---

# Ajouter une langue

1. Copiez `src/TenshoAssistant/Localization/Languages/en.json`.
2. Renommez-le avec le code de la langue, par exemple `es.json`.
3. Mettez à jour `_meta` et traduisez les valeurs.
4. Ne renommez jamais les clés de traduction.
5. Lancez le validateur de localisation depuis la racine du dépôt (demande le SDK .NET indiqué dans `global.json`) :

   ```powershell
   dotnet run --project tests/TenshoAssistant.Tests -c Release -- --validate-localization src/TenshoAssistant/Localization/Languages
   ```

   Les traductions manquantes sont permises et se replient sur l'anglais. Un JSON invalide, des métadonnées, formats ou espaces réservés
   invalides, des clés en double et des clés inconnues doivent être corrigés. `.\scripts\Verify.ps1` lance la même validation, et échoue
   aussi quand une infobulle de réglage nommée dans le code manque dans `en.json` ou `fr.json`.

6. Proposez une pull request vers `Devloppement`. Le formulaire de ticket « Translation » peut servir d'abord à dire quelle langue vous
   prenez.

L'anglais est la langue de référence. Les clés manquantes se replient sur l'anglais, puis sur `[Key.Name]`.

<!-- docgen:languages-fr -->
État actuel (compté dans les fichiers de langue, version <!-- docgen:version -->0.1251<!-- /docgen:version -->) : l'anglais et le français sont complets (7489 clés ; le français doit le rester) ; l'allemand (`de.json`) traduit 30 clés et le japonais (`ja.json`) traduit 30 clés, sans relecture par des locuteurs natifs.
<!-- /docgen:languages-fr -->

L'avancement de chaque langue est
affiché dans Paramètres → Debug et dans le panneau contributeur de Diagnostics (mode développeur).

Les fichiers de langue sont trouvés automatiquement ; aucun changement d'énumération ni de fenêtre C# n'est nécessaire.
Les langues vivent seulement dans `TenshoAssistant.dll` (ressources embarquées, depuis la 0.248) ; le paquet ne livre aucun fichier de
langue. Pour essayer une traduction, créez un dossier `Localization/Languages` à côté de la DLL installée et posez-y le fichier JSON : les
valeurs externes remplacent les valeurs embarquées. Dans Paramètres → Avancé, activez le mode développeur, puis Paramètres → Debug →
Recharger les traductions pour charger les modifications du dossier `Localization/Languages` du plugin installé. Le panneau
contributeur de Diagnostics (mode développeur) peut afficher les clés de traduction. Le rechargement est explicite ; le JSON n'est jamais lu par la recherche normale des traductions pendant
le rendu.

Les formats d'espaces réservés externes invalides sont ignorés au rechargement : la valeur embarquée est gardée, ou l'anglais est utilisé
pour une langue ajoutée partielle. Les diagnostics signalent quand même le fichier modifiable invalide pour qu'il soit corrigé. Les espaces
réservés valides dans un autre ordre sont acceptés, par exemple `{1}` avant `{0}`.

Utilisez des clés descriptives comme `ToolDirectory.Crafting.Name`. N'utilisez pas de libellés numérotés. Gardez les noms d'objets,
d'actions et de lieux de FFXIV dans le catalogue officiel du jeu : ces fichiers traduisent l'interface du plugin, et les données du jeu
s'affichent dans la langue du jeu.
