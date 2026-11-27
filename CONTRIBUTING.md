# Contribuir

## Cambios

- Trabaja en una rama corta a partir de `main`; no integres directamente a `main`.
- Vincula la rama y el pull request con un issue cuando exista.
- Mantén cada commit centrado en un propósito y evita incluir secretos, bases de datos locales, salidas de `bin`/`obj` o artefactos de publicación.
- Antes de abrir un pull request, ejecuta `dotnet test DAS.sln --configuration Release` y revisa `git status` y `git diff`.

## Pull requests

Describe el problema, el cambio, las pruebas ejecutadas y cualquier migración/configuración necesaria. Incluye evidencia visual para cambios de interfaz y redacta datos sensibles. Solicita revisión; integra el cambio solo cuando la CI esté verde y los conflictos estén resueltos.

La CI está definida en `.github/workflows/ci.yml`. Configura GitHub para requerir la comprobación `test` y una revisión aprobada antes de permitir integrar cambios a `main`.
