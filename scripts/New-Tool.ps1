<#
.SYNOPSIS
    Adds the skeleton of a new tool page to Tensho Assistant (TEMPLATES-001).
.DESCRIPTION
    Creates the page, a pure model, a test and the English and French texts, and registers them: the Page enum, the list of pages (MainWindow.Pages.cs), the tests.
    It changes no version and commits nothing. Review the result, then follow "Adding a tool" in CONTRIBUTING.md.
.EXAMPLE
    .\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
#>
param(
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][ValidateSet('Objects','Production','World','Progression','GoldSaucer','Preferences')][string]$Category,
    [Parameter(Mandatory)][string]$LabelEn,
    [Parameter(Mandatory)][string]$LabelFr,
    [switch]$DryRun
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$src=Join-Path $root 'src/TenshoAssistant'
$tests=Join-Path $root 'tests/TenshoAssistant.Tests'
if($Name -cnotmatch '^[A-Z][A-Za-z0-9]{2,39}$'){throw "The name must be in PascalCase, 3 to 40 letters or digits: '$Name'."}
foreach($label in $LabelEn,$LabelFr){if([string]::IsNullOrWhiteSpace($label) -or $label.Length -gt 40){throw "A label must have 1 to 40 characters: '$label'."}}

function Read-Text([string]$path){
    $text=[IO.File]::ReadAllText($path)
    return @{Text=$text.Replace("`r`n","`n");Crlf=$text.Contains("`r`n")}
}
function Write-Text([string]$path,[string]$text,[bool]$crlf){
    if($crlf){$text=$text.Replace("`n","`r`n")}
    [IO.File]::WriteAllText($path,$text,(New-Object Text.UTF8Encoding($false)))
}
function Replace-Once([string]$text,[string]$old,[string]$new,[string]$where){
    $at=$text.IndexOf($old,[StringComparison]::Ordinal)
    if($at -lt 0){throw "Anchor not found in ${where}: $old"}
    return $text.Substring(0,$at)+$new+$text.Substring($at+$old.Length)
}
function Json-Text([string]$value){return ($value | ConvertTo-Json).Trim()}

$pageFile=Join-Path $src "MainWindow.$Name.cs"
$modelFile=Join-Path $src "$Name/${Name}Model.cs"
$testFile=Join-Path $tests "${Name}Tests.cs"
foreach($path in $pageFile,$modelFile,$testFile){if(Test-Path $path){throw "Already exists: $path"}}
$window=Read-Text (Join-Path $src 'MainWindow.cs')
if($window.Text -notmatch 'enum Page \{([^}]*)\}'){throw 'enum Page not found in MainWindow.cs.'}
$pageNames=$Matches[1] -split ',' | ForEach-Object {$_.Trim()}
if($pageNames -contains $Name){throw "The page '$Name' already exists."}

$changes=@(
    "create  src/TenshoAssistant/MainWindow.$Name.cs",
    "create  src/TenshoAssistant/$Name/${Name}Model.cs",
    "create  tests/TenshoAssistant.Tests/${Name}Tests.cs",
    "change  src/TenshoAssistant/MainWindow.cs (enum Page)",
    "change  src/TenshoAssistant/MainWindow.Pages.cs (category $Category)",
    "change  src/TenshoAssistant/Localization/Languages/en.json and fr.json (Navigation.$Name, ${Name}Ui)",
    "change  tests/TenshoAssistant.Tests/Program.cs and TenshoAssistant.Tests.csproj"
)
$changes | ForEach-Object {Write-Host $_}
if($DryRun){Write-Host 'DRY RUN: nothing was written.';return}

# Page
Write-Text $pageFile @"
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// ${Name}: say here what this page does and where its data comes from (a sheet, a probe in docs/evidence/, a cited source).
internal sealed partial class MainWindow
{
    private void Draw$Name()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("${Name}Ui.Title"),localization.Get("${Name}Ui.Help"));
        ImGui.TextDisabled(localization.Format("${Name}Ui.Count",${Name}.${Name}Model.Count([])));
    }
}

"@ $false
# Pure model
New-Item -ItemType Directory -Force (Split-Path $modelFile) | Out-Null
Write-Text $modelFile @"
namespace TenshoAssistant.$Name;

/// <summary>The part of the tool that does not need the game: it is what the tests can check. Code reading the game goes in a file named *.Dalamud.cs.</summary>
public static class ${Name}Model
{
    public static int Count(IEnumerable<string> rows)=>rows.Count(row=>!string.IsNullOrWhiteSpace(row));
}

"@ $false
# Test
Write-Text $testFile @"
using TenshoAssistant.$Name;

internal static class ${Name}Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("${Name}: replace this test with what the tool must do",() =>
        {
            Check(${Name}Model.Count(["a"," ","b"])==2,"blank rows are not counted");
        }),
    ];
}

"@ $false
# Registration
$newWindow=$window.Text -replace '(enum Page \{[^}]*?)\s*\}',('$1, '+$Name+' }')
Write-Text (Join-Path $src 'MainWindow.cs') $newWindow $window.Crlf
$list=Read-Text (Join-Path $src 'MainWindow.Pages.cs')
$anchor='        // New tools: scripts/New-Tool.ps1 adds their line above this one.'
Write-Text (Join-Path $src 'MainWindow.Pages.cs') (Replace-Once $list.Text $anchor "        new(Page.$Name,`"$Category`",PageLabel(`"Navigation.$Name`"),Draw$Name,`"MainWindow.$Name.cs, $Name/`"),`n$anchor" 'MainWindow.Pages.cs') $list.Crlf
# Texts
$texts=@{
    en=@{Label=$LabelEn;Title=$LabelEn;Help='Describe this tool in one sentence.';Count='{0} rows'}
    fr=@{Label=$LabelFr;Title=$LabelFr;Help='Décrivez cet outil en une phrase.';Count='{0} lignes'}
}
foreach($language in 'en','fr'){
    $path=Join-Path $src "Localization/Languages/$language.json"
    $json=Read-Text $path;$row=$texts[$language]
    $head="`n  `"Navigation`": {`n"
    $text=Replace-Once $json.Text $head "$head    `"$Name`": $(Json-Text $row.Label),`n" "$language.json"
    $text=Replace-Once $text $head "`n  `"${Name}Ui`": {`n    `"Title`": $(Json-Text $row.Title),`n    `"Help`": $(Json-Text $row.Help),`n    `"Count`": $(Json-Text $row.Count)`n  },$head" "$language.json"
    Write-Text $path $text $json.Crlf
}
# Tests
$program=Read-Text (Join-Path $tests 'Program.cs')
$chain=[regex]::Matches($program.Text,'\.Concat\((\w+)\.Cases\)')
if($chain.Count -eq 0){throw 'The test list was not found in Program.cs.'}
$last=$chain[$chain.Count-1].Groups[1].Value
Write-Text (Join-Path $tests 'Program.cs') (Replace-Once $program.Text ".Concat($last.Cases)" ".Concat($last.Cases).Concat(${Name}Tests.Cases)" 'Program.cs') $program.Crlf
$project=Read-Text (Join-Path $tests 'TenshoAssistant.Tests.csproj')
$include="    <Compile Include=`"../../src/TenshoAssistant/$Name/*.cs`" Exclude=`"../../src/TenshoAssistant/$Name/*.Dalamud.cs`" Link=`"$Name/%(Filename)%(Extension)`" />`n"
$anchorProject='    <Compile Include="../../src/TenshoAssistant/Localization/*.cs"'
Write-Text (Join-Path $tests 'TenshoAssistant.Tests.csproj') (Replace-Once $project.Text $anchorProject ($include+$anchorProject) 'TenshoAssistant.Tests.csproj') $project.Crlf

Write-Host ''
Write-Host "Done. Next: write the tool and its tests, run .\scripts\Verify.ps1, then follow 'Adding a tool' in CONTRIBUTING.md."
