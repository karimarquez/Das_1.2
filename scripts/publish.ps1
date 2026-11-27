$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$solution = Join-Path $repositoryRoot 'DAS.sln'
$webProject = Join-Path $repositoryRoot 'src\DAS.Web\DAS.Web.csproj'

Push-Location $repositoryRoot
try {
    dotnet test $solution --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Las pruebas fallaron (código $LASTEXITCODE). No se publicó la aplicación."
    }

    dotnet publish $webProject --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "La publicación falló (código $LASTEXITCODE)."
    }
}
finally {
    Pop-Location
}
