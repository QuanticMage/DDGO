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



