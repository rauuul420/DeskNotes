# DeskNotes

Aplicación de escritorio para Windows que permite abrir, crear y editar documentos de texto y mostrarlos en ventanas flotantes sobre el escritorio.

## Objetivo

DeskNotes busca ofrecer una forma sencilla de mantener a la vista notas y documentos personales sin depender de un formato propietario. Los documentos seguirán siendo archivos normales, almacenados en las ubicaciones elegidas por el usuario.

## Estado del proyecto

El proyecto se encuentra en una fase inicial de definición y desarrollo. La primera meta es construir un MVP estable que permita crear, abrir, editar y guardar notas en ventanas independientes.

Consulta la [hoja de ruta](docs/roadmap.md) para conocer las fases previstas y los [requisitos](docs/requirements.md) para ver el alcance funcional.

## Funcionalidades previstas

- Abrir archivos TXT y Markdown.
- Crear notas nuevas.
- Editar y guardar el contenido en los archivos originales.
- Mostrar cada nota en una ventana flotante independiente.
- Mover y redimensionar las ventanas.
- Recordar la posición y el tamaño de las notas.
- Activar o desactivar la opción de mantener una nota siempre visible.

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
├── tests/                       # Se incorporará al crear los proyectos de pruebas
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

## Principios del proyecto

1. Los archivos del usuario son la fuente original del contenido.
2. No se debe introducir una base de datos como único lugar donde se almacenen las notas.
3. La interfaz, la lógica de dominio y el acceso al sistema de archivos deben permanecer separados.
4. Cada funcionalidad debe tener criterios de aceptación y pruebas apropiadas.
5. Los cambios deben ser pequeños, revisables y estar versionados.
6. No se considerará terminada una tarea hasta comprobar su funcionamiento.

## Estado de la documentación

Estos documentos describen el alcance y las decisiones iniciales. Cuando una decisión cambie o una funcionalidad se implemente, la documentación deberá actualizarse para reflejar el estado real del software.
