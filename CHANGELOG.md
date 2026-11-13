# Historial de cambios

Los cambios relevantes de DeskNotes se registrarán aquí. El formato sigue una estructura inspirada en Keep a Changelog y utiliza versionado semántico cuando existan versiones publicadas.

## [Sin publicar]

### Añadido
- Estructura inicial de la solución con proyectos App, Core e Infrastructure.
- Fijación del SDK .NET mediante `global.json`.
- Documentación inicial de requisitos, arquitectura y hoja de ruta.
- ADR-001 para la selección tecnológica.
- ADR-002 para la estrategia de almacenamiento basada en archivos.
- ADR-003 para la persistencia local del estado de las ventanas.
- Guía inicial de contribución.

- Ventana principal para crear notas nuevas y abrir archivos TXT/Markdown.
- Ventana independiente de edición por nota, con guardado y aviso de cambios pendientes.
- Opción para mantener una ventana de nota siempre visible.
- Atajo Ctrl+S para guardar.
- Detección de modificaciones externas mediante metadatos y hash SHA-256.
- Pruebas de almacenamiento y conflicto de modificación externa.
- Integración continua para restaurar, compilar y ejecutar pruebas en Windows.
- Persistencia JSON de las notas abiertas, posiciones, dimensiones y estado «Siempre visible».
- Acción «Guardar como» y atajo Ctrl+Mayús+S para crear una copia de una nota en otra ubicación.
- Mantener la ruta actual de una nota si una operación de guardado falla.
- Extraer la coordinación de guardado y «Guardar como» a `DeskNotes.Core` para poder probarla sin WPF.
- Añadir el proyecto `DeskNotes.Core.Tests` con pruebas de guardado normal, notas nuevas, «Guardar como» y fallos de escritura.
- Añadir la biblioteca persistente «Mis notas», independiente del estado de las ventanas abiertas.
- Registrar automáticamente en la biblioteca las notas abiertas o guardadas, permitir volver a abrirlas y quitar entradas sin borrar archivos.
- Añadir pruebas para comprobar que la biblioteca se conserva aunque no haya ventanas abiertas.
- Añadir widgets flotantes sin borde ni botón en la barra de tareas, con título y vista previa del contenido de una nota.
- Permitir mover, redimensionar, cerrar y abrir en el editor los widgets.
- Añadir opción para mantener cada widget encima de otras ventanas.
- Persistir y restaurar los widgets activos, posición, tamaño y estado «Mantener encima».
- Actualizar el contenido de un widget cuando se guarda su nota.

### Nota de estado
Estas funcionalidades están en una rama de desarrollo. La compilación Release y las 21 pruebas automatizadas pasan en GitHub Actions sobre Windows Server 2025 con .NET SDK 10.0.404; la comprobación manual de la interfaz y la restauración de ventanas sigue pendiente. Todavía no forman parte de una versión publicada.
