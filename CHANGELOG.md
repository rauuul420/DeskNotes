# Historial de cambios

Los cambios relevantes de DeskNotes se registrarán aquí. El formato sigue una estructura inspirada en Keep a Changelog y utiliza versionado semántico cuando existan versiones publicadas.

## [Sin publicar]

### Añadido
- Estructura inicial de la solución con proyectos App, Core e Infrastructure.
- Fijación del SDK .NET mediante `global.json`.
- Documentación inicial de requisitos, arquitectura y hoja de ruta.
- ADR-001 para la selección tecnológica.
- ADR-002 para la estrategia de almacenamiento basada en archivos.
- Guía inicial de contribución.

### Añadido
- Ventana principal para crear notas nuevas y abrir archivos TXT/Markdown.
- Ventana independiente de edición por nota, con guardado y aviso de cambios pendientes.
- Opción para mantener una ventana de nota siempre visible.
- Atajo Ctrl+S para guardar.
- Detección de modificaciones externas mediante metadatos y hash SHA-256.
- Pruebas de almacenamiento y conflicto de modificación externa.
- Integración continua para restaurar, compilar y ejecutar pruebas en Windows.

### Nota de estado
Estas funcionalidades están en una rama de desarrollo y pendientes de validación en Windows; todavía no forman parte de una versión publicada.
