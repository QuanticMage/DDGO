param(
  [string]$Project = "DungeonDefendersGearOptimizer\DDUP.csproj",
  [string]$Source  = "DungeonDefendersGearOptimizer\bin\Release\net8.0\publish\wwwroot",
  [string]$Target  = "docs",
  [string]$Base    = "/DDGO/"
)

dotnet publish $Project -c Release

if (Test-Path $Target) { Remove-Item $Target -Recurse -Force }
New-Item -ItemType Directory -Path $Target | Out-Null
Copy-Item "$Source\*" $Target -Recurse -Force

# Ensure .nojekyll exists in the target directory (GitHub Pages)
$nojekyll = Join-Path $Target ".nojekyll"
New-Item -ItemType File -Path $nojekyll -Force | Out-Null


$index = Join-Path $Target "index.html"
if (!(Test-Path $index)) { throw "index.html not found at $index" }

$c = Get-Content -LiteralPath $index -Raw

# Prefer updating base tag (Blazor)
$c = [regex]::Replace($c, '<base\s+href="[^"]*"\s*/?>', "<base href=""$Base"" />")

Set-Content -LiteralPath $index -Value $c -NoNewline
Write-Host "Updated base href in $index"
