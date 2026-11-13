# Guía de contribución

Gracias por tu interés en DeskNotes. Este documento describe el flujo de trabajo previsto para mantener los cambios claros, pequeños y verificables.

El proyecto se encuentra en una fase inicial. Algunas normas podrán ampliarse cuando se incorporen pruebas automatizadas y procesos de integración continua.

## Antes de empezar

1. Lee el [README](README.md).
2. Consulta los [requisitos](docs/requirements.md) y la [arquitectura](docs/architecture.md).
3. Comprueba las decisiones técnicas registradas en `docs/decisions/`.
4. Asegúrate de trabajar sobre la versión más reciente de la rama principal.

## Entorno de desarrollo

- Windows.
- Visual Studio Code.
- .NET SDK 10.0.404.
- Git.
- SourceGit es el cliente gráfico recomendado para revisar el estado, ramas y commits.

El repositorio incluye `global.json` para fijar la versión del SDK.

## Flujo de trabajo

Se utilizarán ramas cortas para cada cambio:

- `feature/nombre`: nuevas funcionalidades.
- `fix/nombre`: correcciones.
- `docs/nombre`: documentación.
- `refactor/nombre`: reorganización interna sin cambiar el comportamiento esperado.

La rama `main` debe mantenerse estable. Los cambios se prepararán en ramas separadas y se revisarán antes de integrarse.

## Commits

Los commits deben ser pequeños y describir claramente el cambio.

Ejemplos:

- `docs: add initial product requirements`
- `feat: create note window`
- `fix: handle missing note file`
- `test: cover text file loading`

Se recomienda separar cambios de documentación, funcionalidad y refactorización cuando sea razonable.

## Comprobaciones antes de integrar

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet build DeskNotes.slnx
dotnet test DeskNotes.slnx
```

Mientras no existan proyectos de pruebas, `dotnet test` no sustituye las comprobaciones manuales de la aplicación. Ejecuta también la aplicación en Windows cuando el cambio afecte a su comportamiento:

```powershell
dotnet run --project src/DeskNotes.App/DeskNotes.App.csproj
```

No se debe afirmar que una función está probada si únicamente se ha compilado.

## Criterios para considerar un cambio listo

- El cambio cumple el objetivo descrito.
- La compilación termina correctamente.
- Las pruebas relevantes pasan, si existen.
- Se han realizado las comprobaciones manuales necesarias.
- No se han introducido dependencias innecesarias.
- La documentación afectada está actualizada.
- Los cambios son revisables y tienen commits descriptivos.

## Documentación y decisiones

Si un cambio altera requisitos, arquitectura, instrucciones de uso o decisiones técnicas, actualiza los documentos correspondientes.

Las decisiones arquitectónicas importantes deben registrarse como ADR en `docs/decisions/`, explicando contexto, decisión, alternativas y consecuencias.

## Protección de datos

No añadas al repositorio documentos personales, credenciales, tokens, contraseñas ni archivos locales de configuración que contengan secretos. Revisa siempre los cambios antes de hacer commit o push.
