# Historial de cambios

Los cambios relevantes de DeskNotes se registran siguiendo una estructura inspirada en Keep a Changelog.

## [0.1.0-alpha] - 2026-11-13

Primera versión preliminar de DeskNotes para validar el MVP en Windows.

### Añadido
- Aplicación WPF para crear notas nuevas y abrir archivos TXT y Markdown.
- Edición de notas en ventanas independientes y guardado en los archivos originales.
- «Guardar como» y atajos Ctrl+S y Ctrl+Mayús+S.
- Avisos de cambios sin guardar y detección de modificaciones externas.
- Opción para mantener las ventanas de notas siempre visibles.
- Persistencia y restauración de ventanas, posiciones y dimensiones.
- Biblioteca persistente «Mis notas», independiente de las ventanas abiertas.
- Widgets flotantes de escritorio con título y vista previa del contenido.
- Posibilidad de mover, redimensionar, cerrar y mantener encima los widgets.
- Apertura del editor de la nota desde el widget mediante doble clic en la parte superior o el botón «Editar».
- Persistencia y restauración de widgets y sus preferencias.
- Pruebas automatizadas para lógica de almacenamiento, biblioteca y guardado.
- Instalador de Windows mediante NSIS, con detección de .NET 10 Desktop Runtime x64, accesos directos y desinstalador.

### Limitaciones conocidas
- La edición del contenido directamente dentro del widget no está implementada. Para editar, el widget abre el editor de la nota.
- Los widgets son ventanas WPF flotantes; todavía no se integran en la superficie del escritorio detrás de los iconos.
- No hay guardado automático ni vista previa renderizada de Markdown.
- El instalador requiere que Microsoft .NET 10 Desktop Runtime x64 esté instalado previamente; no distribuye el runtime.

### Validación
- Compilación Release y pruebas automatizadas completadas correctamente en GitHub Actions (Windows Server 2025, .NET SDK 10.0.404).
- Flujos principales y restauración de ventanas/widgets comprobados manualmente en Windows.
- Instalador NSIS compilado y ejecutado manualmente en Windows con .NET 10 Desktop Runtime x64 instalado.

## [Sin publicar]

### Añadido
- Opción experimental para integrar los widgets en la capa del escritorio, entre el fondo y los iconos, compatible mediante rutas Win32 diferenciadas para las composiciones clásicas y modernas de Windows 10/11.
- Persistencia de la preferencia de capa del escritorio y aplicación automática a widgets existentes y nuevos.

### Notas técnicas
- La integración depende de detalles internos no documentados de Explorer; si no se detecta una jerarquía compatible, DeskNotes conserva los widgets como ventanas normales.
- El control «Mantener encima» queda temporalmente deshabilitado mientras un widget está en la capa del escritorio y recupera su estado al salir de ella.

### Añadido
- Preferencia de guardado automático para notas vinculadas a archivos, desactivada por defecto.
- Intervalos configurables de 5, 10, 30 o 60 segundos desde la última edición.
- Persistencia de las preferencias de guardado automático entre sesiones.

Los cambios posteriores a esta versión se registrarán aquí.
