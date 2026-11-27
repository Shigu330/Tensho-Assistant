# PluginUiTest (pinned copy of its library)

> **Test tool, not affiliated with the Dalamud team, does not replace Dalamud.**

*Version française plus bas.*

PluginUiTest, the tool that draws every window of a Dalamud plugin without the game and reports what breaks, now lives in its own
repository: **[Shigu330/PluginUiTest](https://github.com/Shigu330/PluginUiTest)** (GPL v3). The tool, its sample plugin, its CI example,
its JSON report schema and its releases are there; it is the source.

## What stays here

Only `src/PluginUiTest.Host`, the tool's library, as a **pinned copy**: Tensho's desktop application (`src/TenshoAssistant.Desktop`)
references it by `ProjectReference`, so this repository builds without the other one. Pinned at PluginUiTest **v0.2.0** plus commit
`602838a` (main, not released yet: the `stamp` and `clock` options of `Rewrite.Plugin`, `HostClock`, `Win32Gl.Resize`, icons not
oversampled, up to commit `e52a707`; then `FrameworkProxy.KeepWork`: the work handed to `IFramework` is kept only for a host that runs it,
otherwise its task is already complete, as before 0.2.0); its `.cs` files are the same as there (only the project file's comment differs). It is never edited here first: a change goes
to Shigu330/PluginUiTest, then is copied back with the new version and commit written above.

## Licence

GPL v3, like Tensho Assistant. Nothing of Dalamud or of the game is copied: every Dalamud, Lumina, ImGui, HexaGen and Mono.Cecil reference
is `Private=false` and resolved at run time from the user's install.

---

# PluginUiTest (copie figée de sa bibliothèque)

> **Outil de test, non affilié à l'équipe de Dalamud, ne remplace pas Dalamud.**

PluginUiTest, l'outil qui dessine chaque fenêtre d'un plugin Dalamud sans le jeu et dit ce qui casse, vit maintenant dans son propre
dépôt : **[Shigu330/PluginUiTest](https://github.com/Shigu330/PluginUiTest)** (GPL v3). L'outil, son plugin d'exemple, son exemple de
CI, le schéma JSON de son rapport et ses releases y sont ; c'est la source.

## Ce qui reste ici

Seulement `src/PluginUiTest.Host`, la bibliothèque de l'outil, en **copie figée** : l'application de bureau de Tensho
(`src/TenshoAssistant.Desktop`) la référence par `ProjectReference`, ce dépôt se compile donc sans l'autre. Figée à PluginUiTest
**v0.2.0** plus le commit `602838a` (main, pas encore publié : les options `stamp` et `clock` de `Rewrite.Plugin`, `HostClock`,
`Win32Gl.Resize`, icônes non suréchantillonnées, jusqu'au commit `e52a707` ; puis `FrameworkProxy.KeepWork` : le travail donné à
`IFramework` n'est gardé que pour un hôte qui le lance, sinon sa tâche est déjà finie, comme avant la 0.2.0) ; ses fichiers `.cs` sont les mêmes que là-bas (seul le commentaire du fichier de projet diffère). Elle ne se
modifie jamais d'abord ici : un changement va dans Shigu330/PluginUiTest, puis se recopie avec la nouvelle version et le commit écrits
plus haut.

## Licence

GPL v3, comme Tensho Assistant. Rien de Dalamud ni du jeu n'est copié : chaque référence à Dalamud, Lumina, ImGui, HexaGen et Mono.Cecil
est `Private=false` et résolue à l'exécution dans l'installation de l'utilisateur.
