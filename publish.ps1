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

# GitHub Pages SPA routing: copy index.html as 404.html so direct navigation
# to Blazor routes (e.g. /dbsearch) loads the app instead of returning a 404.
Copy-Item (Join-Path $Target "index.html") (Join-Path $Target "404.html") -Force



