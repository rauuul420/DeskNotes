# ADR-001: Selección de la tecnología principal

- **Estado:** aceptada
- **Fecha:** 2026-11-13
- **Decisores:** responsable del proyecto
- **Contexto:** DeskNotes es una aplicación de escritorio cuyo primer objetivo es Windows.

## Contexto

DeskNotes necesita crear ventanas independientes, controles de edición de texto, integración con el sistema de ventanas de Windows y acceso al sistema de archivos.

El proyecto se plantea como una aplicación para Windows, por lo que no es imprescindible mantener compatibilidad multiplataforma en el MVP.

También se busca un entorno que permita aprender C# y .NET, compilar desde línea de comandos y trabajar con un editor ligero.

## Decisión

Se adopta el siguiente conjunto tecnológico:

- C# como lenguaje principal.
- .NET 10 como plataforma de ejecución y compilación.
- WPF como framework de interfaz de escritorio.
- XAML para definir las vistas.
- MVVM como patrón de separación de presentación.
- CommunityToolkit.Mvvm como toolkit previsto para reducir código repetitivo de MVVM.
- xUnit como framework previsto para pruebas automatizadas.
- Visual Studio Code como editor.
- Git y SourceGit para control de versiones.
- GitHub como repositorio remoto.

La solución fija el SDK mediante `global.json` en la versión `10.0.404`.

## Alternativas consideradas

### C# + WPF

Ventajas:
- Integración madura con Windows.
- Modelo de ventanas y controles adecuado para una aplicación de escritorio.
- XAML permite separar diseño y lógica de presentación.
- Buen encaje con C# y .NET.

Inconveniente:
- Es una tecnología específica de Windows.

### C# + WinUI 3

Ventaja:
- Tecnología de interfaz moderna dentro del ecosistema Windows.

Inconveniente:
- Añade complejidad inicial que no es necesaria para validar el MVP.

### C# + Avalonia

Ventaja:
- Permite desarrollar aplicaciones multiplataforma.

Inconveniente:
- El objetivo actual es Windows y algunas integraciones específicas del sistema pueden requerir trabajo adicional.

### Python + Qt

Ventaja:
- Permite construir interfaces de escritorio con rapidez.

Inconveniente:
- C# y .NET ofrecen un encaje más directo con la plataforma Windows elegida para este proyecto.

### JavaScript + Electron

Ventaja:
- Flexibilidad para construir interfaces con tecnologías web.

Inconveniente:
- No es la opción preferida para una utilidad de escritorio ligera y estrechamente integrada con Windows.

## Consecuencias

Positivas:
- Se utiliza una tecnología madura para ventanas y controles de Windows.
- La aplicación puede aprovechar las APIs de Windows cuando sean necesarias.
- La lógica puede separarse de la interfaz mediante MVVM.
- El proyecto puede compilarse mediante el SDK de .NET desde VS Code.

Negativas:
- La interfaz WPF limita el objetivo principal a Windows.
- El desarrollo y las pruebas de la interfaz deben realizarse en un entorno Windows compatible.

## Revisión

La decisión podrá revisarse si el proyecto adopta un objetivo multiplataforma o si aparece una limitación técnica importante de WPF. Cualquier cambio deberá documentarse en un nuevo ADR o mediante la actualización explícita de este registro.
