# DeskNotes

Aplicación de escritorio para Windows que permite abrir, crear y editar documentos de texto y mostrarlos en ventanas flotantes sobre el escritorio.

## Objetivo

DeskNotes busca ofrecer una forma sencilla de mantener a la vista notas y documentos personales sin depender de un formato propietario. Los documentos seguirán siendo archivos normales, almacenados en las ubicaciones elegidas por el usuario.

## Estado del proyecto

DeskNotes 0.1.0-alpha es la primera versión preliminar del MVP. Incluye creación, apertura y edición de archivos TXT/Markdown, biblioteca persistente «Mis notas», restauración de ventanas y widgets flotantes de escritorio. La compilación Release y las pruebas automatizadas pasan en GitHub Actions sobre Windows Server 2025 con .NET SDK 10.0.404. La interfaz y los flujos principales también se han validado manualmente en Windows.

Consulta la [hoja de ruta](docs/roadmap.md) para conocer las fases previstas y los [requisitos](docs/requirements.md) para ver el alcance funcional.

## Funcionalidades incluidas en la Alpha

- Crear notas nuevas y abrir archivos TXT y Markdown.
- Editar y guardar el contenido en archivos normales UTF-8.
- Guardar una nota en otra ubicación o con otro nombre mediante «Guardar como».
- Usar Ctrl+S para guardar y Ctrl+Mayús+S para «Guardar como».
- Mostrar cada nota en una ventana independiente.
- Avisar de cambios sin guardar antes de cerrar.
- Detectar modificaciones externas del archivo antes de sobrescribirlo.
- Activar o desactivar la opción de mantener una nota siempre visible.
- Recordar las posiciones y tamaños de las ventanas.
- Restaurar las notas abiertas en el siguiente inicio.
- Mantener una biblioteca «Mis notas» independiente de las ventanas abiertas.
- Crear widgets flotantes de escritorio con título y vista previa del contenido de una nota.
- Mover y redimensionar widgets, abrir su nota en el editor y elegir si permanecen encima de otras ventanas.
- Restaurar los widgets y su posición al iniciar DeskNotes.

El guardado automático y la vista previa de Markdown todavía no están implementados. Los widgets se muestran como ventanas compactas sin botón en la barra de tareas; no se incrustan en la capa del escritorio detrás de los iconos. La edición del contenido se realiza en el editor de la nota: el doble clic en la parte superior del widget y el botón «Editar» abren dicho editor. La edición directa dentro del widget queda aplazada. La biblioteca «Mis notas» guarda rutas y títulos en la configuración local; el contenido continúa en los archivos originales.

Las funciones avanzadas —como vista previa Markdown, transparencia, temas, etiquetas, búsqueda y sincronización— se incorporarán después del MVP.

## Tecnologías

- **Lenguaje:** C#.
- **Plataforma:** .NET 10.
- **Interfaz de escritorio:** Windows Presentation Foundation (WPF) y XAML.
- **Arquitectura de presentación:** MVVM.
- **Toolkit MVVM previsto:** CommunityToolkit.Mvvm.
- **Pruebas previstas:** xUnit.
- **Control de versiones:** Git y GitHub.
- **Cliente Git:** SourceGit.
- **Editor:** Visual Studio Code.

La decisión y el razonamiento de esta selección están documentados en [ADR-001](docs/decisions/ADR-001-technology-stack.md).

## Estructura del repositorio

```text
DeskNotes/
├── docs/
│   ├── decisions/
│   │   ├── ADR-001-technology-stack.md
│   │   └── ADR-002-file-storage.md
│   ├── architecture.md
│   ├── requirements.md
│   └── roadmap.md
├── src/
│   ├── DeskNotes.App/
│   ├── DeskNotes.Core/
│   └── DeskNotes.Infrastructure/
├── tests/
│   ├── DeskNotes.Core.Tests/
│   └── DeskNotes.Infrastructure.Tests/
├── CHANGELOG.md
├── CONTRIBUTING.md
├── DeskNotes.slnx
└── README.md
```

## Documentación

- [Requisitos del producto](docs/requirements.md)
- [Hoja de ruta](docs/roadmap.md)
- [Arquitectura técnica](docs/architecture.md)
- [ADR-001: selección tecnológica](docs/decisions/ADR-001-technology-stack.md)
- [ADR-002: almacenamiento basado en archivos](docs/decisions/ADR-002-file-storage.md)
- [ADR-003: persistencia del estado de ventanas](docs/decisions/ADR-003-window-state-persistence.md)
- [Guía de contribución](CONTRIBUTING.md)
- [Historial de cambios](CHANGELOG.md)

## Requisitos para desarrollar

- Windows con soporte para aplicaciones WPF.
- .NET SDK 10.0.404, según la versión fijada en `global.json`.
- Visual Studio Code.
- Git.
- SourceGit (opcional, como interfaz gráfica para Git).

## Compilar y ejecutar

Desde la carpeta raíz del repositorio:

```powershell
dotnet restore
dotnet build DeskNotes.slnx
dotnet run --project src/DeskNotes.App/DeskNotes.App.csproj
```

La compilación y la ejecución deben realizarse en Windows, ya que el proyecto de interfaz utiliza WPF.

## Empaquetado para Windows

La distribución se prepara mediante NSIS. El instalador final se denomina \`DeskNotes-Setup-0.1.0.exe\` y realiza una instalación para todos los usuarios en \`C:\Program Files\DeskNotes\`, por lo que solicita permisos de administrador.

La publicación genera un único ejecutable de aplicación para \`win-x64\` con .NET 10 framework-dependent (\`SelfContained=false\`, \`PublishSingleFile=true\`). **No incluye el runtime de .NET**: el equipo de destino debe disponer de \`Microsoft .NET 10 Desktop Runtime x64\`. El instalador comprueba la presencia de la versión 10 en la ubicación estándar de .NET x64. Si no la encuentra, ofrece abrir la página oficial de descarga y cancela esa ejecución; después de instalar el runtime, hay que volver a ejecutar el instalador.

La desinstalación elimina los archivos de la aplicación y sus accesos directos, pero conserva \`%LocalAppData%\DeskNotes\`, donde se guardan los ajustes del usuario.

En Windows, con .NET SDK 10.0.404 y NSIS instalados, ejecutar desde la raíz:

\`\`\`powershell
./packaging/publish.ps1
New-Item -ItemType Directory -Force artifacts/installer
$nsisPath = Join-Path ${env:ProgramFiles(x86)} "NSIS/makensis.exe"
& $nsisPath /DAPP_VERSION=0.1.0-alpha /DAPP_VERSION_NUM=0.1.0.0 /DAPP_FILE_VERSION=0.1.0 "/DPUBLISH_DIR=$PWD\artifacts\publish" "/DOUTPUT_DIR=$PWD\artifacts\installer" packaging/nsis/DeskNotes.nsi
\`\`\`

El instalador se genera en \`artifacts/installer/DeskNotes-Setup-0.1.0.exe\`. La compilación automatizada se encuentra en \`.github/workflows/installer.yml\` y publica el instalador como artefacto de GitHub Actions.

## Principios del proyecto

1. Los archivos del usuario son la fuente original del contenido.
2. No se debe introducir una base de datos como único lugar donde se almacenen las notas.
3. La interfaz, la lógica de dominio y el acceso al sistema de archivos deben permanecer separados.
4. Cada funcionalidad debe tener criterios de aceptación y pruebas apropiadas.
5. Los cambios deben ser pequeños, revisables y estar versionados.
6. No se considerará terminada una tarea hasta comprobar su funcionamiento.

## Versión

**0.1.0-alpha** — primera versión preliminar para pruebas. Consulta [CHANGELOG.md](CHANGELOG.md) para conocer las novedades y limitaciones.

## Estado de la documentación

Estos documentos describen el alcance y las decisiones iniciales. Cuando una decisión cambie o una funcionalidad se implemente, la documentación deberá actualizarse para reflejar el estado real del software.
