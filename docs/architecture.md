# Arquitectura técnica

**Proyecto:** DeskNotes  
**Estado:** diseño inicial  
**Plataforma objetivo:** Windows 11  
**Framework:** .NET 10  
**Interfaz:** WPF

## 1. Objetivos de arquitectura

La arquitectura debe permitir desarrollar DeskNotes de forma incremental, mantener separadas las responsabilidades y evitar que la lógica de negocio dependa de la interfaz gráfica o de detalles concretos del sistema de archivos.

Principios:

- Separación de responsabilidades.
- Dependencias dirigidas hacia el núcleo del dominio.
- Archivos del usuario como fuente original del contenido.
- Pruebas automatizadas para la lógica que pueda probarse sin interfaz.
- Complejidad proporcional a las necesidades reales del producto.

No se introducirán capas, abstracciones o paquetes sin una necesidad concreta.

## 2. Tecnologías

| Área | Tecnología |
|---|---|
| Lenguaje | C# |
| Runtime | .NET 10 |
| Aplicación de escritorio | WPF |
| Lenguaje de interfaz | XAML |
| Patrón de presentación | MVVM |
| Toolkit MVVM previsto | CommunityToolkit.Mvvm |
| Pruebas | xUnit |
| Editor | Visual Studio Code |
| Control de versiones | Git |
| Cliente Git | SourceGit |
| Repositorio remoto | GitHub |

La selección tecnológica está justificada en [ADR-001](decisions/ADR-001-technology-stack.md).

## 3. Estructura de la solución

La solución existente utiliza el formato `.slnx` y contiene estos proyectos:

```text
DeskNotes/
├── DeskNotes.slnx
├── global.json
├── src/
│   ├── DeskNotes.App/
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── NoteWindow.xaml
│   │   ├── NoteWindow.xaml.cs
│   │   └── DeskNotes.App.csproj
│   ├── DeskNotes.Core/
│   │   └── DeskNotes.Core.csproj
│   └── DeskNotes.Infrastructure/
│       └── DeskNotes.Infrastructure.csproj
└── tests/
    └── DeskNotes.Infrastructure.Tests/
```

Estructura objetivo progresiva:

```text
DeskNotes/
├── src/
│   ├── DeskNotes.App/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Controls/
│   │   ├── Resources/
│   │   └── App.xaml
│   ├── DeskNotes.Core/
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   └── Services/
│   └── DeskNotes.Infrastructure/
│       ├── FileSystem/
│       ├── Persistence/
│       └── Services/
├── tests/
│   ├── DeskNotes.Core.Tests/
│   └── DeskNotes.Infrastructure.Tests/
└── docs/
```

Las carpetas se crearán cuando haya archivos que deban contener. No es necesario crear directorios vacíos por adelantado.

## 4. Responsabilidades por proyecto

### DeskNotes.Core

Contiene los conceptos y reglas principales del dominio.

Responsabilidades previstas:

- Representar una nota y sus metadatos de dominio.
- Definir contratos que necesite el resto de la aplicación.
- Mantener reglas que no dependan de WPF ni del sistema de archivos.
- Facilitar pruebas unitarias independientes.

Core no debe referenciar App ni Infrastructure.

### DeskNotes.Infrastructure

Implementa las operaciones técnicas externas al dominio.

Responsabilidades previstas:

- Leer y escribir archivos.
- Resolver operaciones sobre rutas.
- Gestionar detalles de codificación de texto.
- Persistir preferencias y estado de ventanas mediante JSON en `%LocalAppData%/DeskNotes/settings.json`.
- Implementar las interfaces declaradas por Core cuando corresponda.

Infrastructure puede depender de Core, pero Core no debe depender de Infrastructure.

### DeskNotes.App

Es el punto de entrada y la interfaz gráfica de la aplicación.

Responsabilidades previstas:

- Inicializar la aplicación WPF.
- Mostrar la ventana principal y las ventanas de notas.
- Presentar controles y recursos visuales.
- Conectar Views con ViewModels.
- Configurar e integrar las implementaciones de servicios.

App puede depender de Core e Infrastructure.

## 5. Presentación: MVVM

Se utilizará MVVM para separar la interfaz de la lógica de presentación.

- **View:** archivos XAML y controles visuales. Presentan la información y enlazan sus propiedades con el ViewModel.
- **ViewModel:** estado de presentación, propiedades enlazables y comandos que responden a las acciones del usuario.
- **Model:** objetos que representan los conceptos de la aplicación y sus datos.

CommunityToolkit.Mvvm se incorporará cuando comencemos a implementar ViewModels. No es necesario añadirlo antes de que exista una funcionalidad que lo utilice.

El code-behind de WPF se reservará para tareas propias de la vista que no encajen razonablemente en el enlace de datos o en los comandos. Las reglas de negocio no deben residir allí.

## 6. Modelo conceptual de una nota

Como punto de partida, una nota necesitará representar al menos:

- Identificador interno de la sesión o de la nota.
- Ruta del archivo asociado, si existe.
- Contenido de texto.
- Estado de cambios pendientes de guardar.
- Estado de la ventana (posición y dimensiones), tratado como preferencia de presentación.

Este es un modelo conceptual, no una definición definitiva de clases. Los tipos y propiedades concretos se decidirán al implementar los primeros casos de uso.

Una nota nueva que todavía no se haya guardado necesitará poder existir sin una ruta de archivo definitiva.

## 7. Gestión de archivos y persistencia

Los archivos TXT y MD son la fuente original del contenido. DeskNotes debe leerlos y escribirlos sin exigir que el usuario los importe a una base de datos propia.

Se distinguirán dos tipos de información:

1. **Contenido de la nota:** se guarda en el archivo elegido por el usuario.
2. **Preferencias de la aplicación:** pueden guardarse en almacenamiento local separado, por ejemplo posición y tamaño de ventanas.

El servicio actual lee y escribe TXT/MD como UTF-8, detecta BOM al abrir, escribe sin BOM y utiliza un archivo temporal antes de sustituir el destino. El servicio `JsonAppSettingsService` persiste las rutas de las notas abiertas y sus posiciones, dimensiones y estado `Topmost` en `%LocalAppData%/DeskNotes/settings.json`, usando también un archivo temporal para sustituir la configuración. Al abrir una nota registra fecha, tamaño y hash SHA-256 del contenido; antes de guardar una nota existente compara esos datos para detectar modificaciones externas. Esta comprobación reduce el riesgo de sobrescritura, aunque no elimina por completo una posible carrera si otro proceso modifica el archivo exactamente entre la comprobación y el reemplazo.

La decisión se documenta en [ADR-002](decisions/ADR-002-file-storage.md).

## 8. Flujo de dependencias

Dirección conceptual:

```text
DeskNotes.App
   ├──> DeskNotes.Core
   └──> DeskNotes.Infrastructure
             └──> DeskNotes.Core

DeskNotes.Core.Tests
   └──> DeskNotes.Core

DeskNotes.Infrastructure.Tests
   └──> DeskNotes.Infrastructure
        └──> DeskNotes.Core
```

El proyecto `DeskNotes.Infrastructure.Tests` ya forma parte de la solución y contiene pruebas del servicio de archivos. Las pruebas de Core y de interfaz se añadirán cuando existan reglas y comportamientos que justifiquen cubrirlas.

## 9. Pruebas

- Core: pruebas unitarias de modelos y reglas.
- Infrastructure: pruebas de lectura, escritura, rutas y errores usando ubicaciones temporales.
- App: comprobaciones de integración y pruebas manuales de la interfaz; se evaluará automatización de UI si aporta valor.

Las pruebas no deben depender de documentos personales ni modificar archivos del usuario.

## 10. Decisiones pendientes

Antes de implementar las funciones correspondientes habrá que concretar:

- Formato de configuración local y ubicación de los datos de la aplicación.
- Estrategia de detección de cambios externos.
- Política de codificaciones y conservación de finales de línea.
- Estrategia de guardado seguro y recuperación ante errores.
- Comportamiento al cerrar notas con cambios pendientes.
- Gestión de pantallas múltiples y posiciones fuera del área visible.
- Ciclo de vida y restauración de ventanas.
- Nivel exacto de compatibilidad de Windows que se publicará.

Estas decisiones se documentarán como ADR cuando se tomen. La arquitectura es una guía evolutiva y deberá reflejar el código real, no una estructura ideal que todavía no existe.
