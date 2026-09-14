<#
.SYNOPSIS
    Regenerates ocr/rune-combinations.json from poe2db's Runeshape Combinations page.

.DESCRIPTION
    The Combinations panel draws each combination's runes in the order poe2db lists them, and the
    app resolves a gilded rune from (row name, icon count, cell index) against this table. Run this
    after a patch changes recipes; the result is embedded into the executable at build time.

    Each combination card on the page carries the result name (currency names sit behind an <img>
    inside the link), an optional "(Level N)" and/or "xN" quantity suffix, a level tier ("Lv70+",
    "Lv65-74") and one <a href="Oath_Rune"><img/></a> per rune. Runes are read from the link
    target, not the icon tooltip: the tooltip only carries "Level 70 - 100 Oath Rune" on some
    icons and a bare " Oath Rune" on others, and a parser keyed on the former silently dropped up to
    nine runes from a ten-rune recipe. Rune names are mapped to the catalog ids in
    ocr/rune-catalog.json; an unknown rune name fails the run rather than shipping a hole.

.EXAMPLE
    .\scripts\update-rune-combinations.ps1
#>
param(
    [string]$Url = "https://poe2db.tw/Runeshape_Combinations",

    # Parse a saved copy of the page instead of downloading it.
    [string]$FromFile
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$outPath = Join-Path $repoRoot "ocr\rune-combinations.json"
$catalogPath = Join-Path $repoRoot "ocr\rune-catalog.json"

$catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
$idsByName = @{}
foreach ($rune in $catalog.runes) { $idsByName[$rune.displayName] = $rune.id }

if ($FromFile) {
    $html = Get-Content -LiteralPath $FromFile -Raw -Encoding UTF8
} else {
    Write-Host "Downloading $Url..."
    $html = (Invoke-WebRequest -Uri $Url -UseBasicParsing -Headers @{ "User-Agent" = "RuneshapePriceChecker/update-rune-combinations" }).Content
}

$cards = $html -split '<div class="col"><div class="d-flex border-top rounded">'
if ($cards.Count -lt 2) { throw "No combination cards found; the page markup may have changed." }

$nameRegex = [regex]'<span><a [^>]*>(?:<img[^>]*/?>)?([^<]+)</a>([^<]*)</span>\s*<span class="default small">([^<]*)</span>'
$runeRegex = [regex]'<a href="([A-Za-z_]+_Rune)"><img'
$levelRegex = [regex]'\(Level (\d+)\)'
$quantityRegex = [regex]'\bx(\d+)\b'

$seen = @{}
$combos = New-Object System.Collections.Generic.List[object]
foreach ($card in $cards | Select-Object -Skip 1) {
    $m = $nameRegex.Match($card)
    if (-not $m.Success) { continue }
    $runeNames = @($runeRegex.Matches($card) | ForEach-Object { $_.Groups[1].Value.Replace('_', ' ') })
    if ($runeNames.Count -eq 0) { continue }

    $name = [System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value).Trim()
    $suffix = [System.Net.WebUtility]::HtmlDecode($m.Groups[2].Value).Trim()
    $tier = $m.Groups[3].Value.Trim()
    $level = 0
    $quantity = 0
    $lm = $levelRegex.Match($suffix)
    $qm = $quantityRegex.Match($suffix)
    if ($lm.Success) { $level = [int]$lm.Groups[1].Value }
    if ($qm.Success) { $quantity = [int]$qm.Groups[1].Value }
    if (-not $lm.Success -and -not $qm.Success -and $suffix) { $name = "$name $suffix" }  # "Unique" + "Ring": the class is part of the name

    $ids = foreach ($runeName in $runeNames) {
        if (-not $idsByName.ContainsKey($runeName)) { throw "Unknown rune '$runeName' in '$name' - add it to ocr/rune-catalog.json first." }
        $idsByName[$runeName]
    }

    $key = "$name|$level|$quantity|$($ids -join ',')"
    if ($seen.ContainsKey($key)) { continue }
    $seen[$key] = $true
    $combos.Add([ordered]@{ name = $name; level = $level; quantity = $quantity; tier = $tier; runes = @($ids) })
}

if ($combos.Count -lt 300) { throw "Only $($combos.Count) combinations parsed; expected a few hundred." }

$sorted = @($combos | Sort-Object -Property { $_.name }, { $_.level }, { $_.quantity })
$document = [ordered]@{
    source = $Url
    fetchedUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    combinations = @($sorted)
}

# One combination per line keeps the diff readable when a recipe changes.
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("{")
$lines.Add("  ""source"": ""$($document.source)"",")
$lines.Add("  ""fetchedUtc"": ""$($document.fetchedUtc)"",")
$lines.Add("  ""combinations"": [")
$last = $sorted.Count - 1
for ($i = 0; $i -le $last; $i++) {
    $c = $sorted[$i]
    $runeList = ($c.runes | ForEach-Object { """$_""" }) -join ", "
    $escapedName = $c.name.Replace('\', '\\').Replace('"', '\"')
    $comma = if ($i -lt $last) { "," } else { "" }
    $lines.Add("    { ""name"": ""$escapedName"", ""level"": $($c.level), ""quantity"": $($c.quantity), ""tier"": ""$($c.tier)"", ""runes"": [$runeList] }$comma")
}
$lines.Add("  ]")
$lines.Add("}")

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($outPath, ($lines -join "`n") + "`n", $utf8NoBom)
Write-Host "Wrote $($sorted.Count) combinations to $outPath"
