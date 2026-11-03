# Adding a language

1. Copy `src/TenshoAssistant/Localization/Languages/en.json`.
2. Rename it with the language code, for example `es.json`.
3. Update `_meta` and translate the values.
4. Never rename translation keys.
5. Run the localization validator from the repository root (requires the .NET SDK specified in `global.json`):

   ```powershell
   dotnet run --project tests/TenshoAssistant.Tests -c Release -- --validate-localization src/TenshoAssistant/Localization/Languages
   ```

   Missing translations are allowed and fall back to English. Invalid JSON,
   metadata, formats, placeholders, duplicate keys and unknown keys must be fixed.
   `.\scripts\Verify.ps1` runs the same validation, and also fails when a setting tooltip named in the code is missing in
   `en.json` or `fr.json`.

6. Submit a pull request.

English is the reference language. Missing keys fall back to English, then to `[Key.Name]`.

Current state (2026-11-02): English and French are complete (French must stay complete); German (`de.json`) and Japanese
(`ja.json`) only translate a few dozen keys and have not been reviewed by native speakers. The completion of each language is
shown in Settings → Debug and in the contributor panel of Diagnostics (developer mode).

Language files are discovered automatically; no enum or C# window changes are needed.
Since 0.248 the languages live only inside `TenshoAssistant.dll` (embedded resources); the package ships no
language file. To test a translation, create a `Localization/Languages` folder next to the installed DLL and put
the JSON file there: external values override bundled values.
Use Settings → Advanced to enable Developer mode, then Settings → Debug → Reload translations
to load edits from the installed plugin's `Localization/Languages` directory.
The same panel can show translation keys. Reloading is explicit; JSON is never
parsed by the normal translation lookup during rendering.

Invalid external placeholder formats are ignored at reload: the bundled value
is retained, or English is used for a partial added language. The diagnostics
still report the invalid editable file so it can be corrected. Valid reordered
placeholders are supported, for example `{1}` before `{0}`.

Use descriptive keys such as `ToolDirectory.Crafting.Name`. Do not use numbered
labels. Keep FFXIV item, action and location names in the official game catalog;
these files translate the plugin interface.
