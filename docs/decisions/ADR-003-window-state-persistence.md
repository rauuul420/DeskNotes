# ADR-003: persistir el estado de las ventanas en JSON local

- **Estado:** aceptada para el MVP
- **Fecha:** 2026-11-13

## Contexto

DeskNotes debe recordar qué notas estaban abiertas y restaurar su posición, tamaño y preferencia «Siempre visible» en el siguiente inicio. El contenido de las notas debe seguir almacenándose en sus archivos TXT/Markdown originales; la configuración de la aplicación no debe convertirse en un segundo almacén del contenido.

## Decisión

Guardar la configuración de sesión en:

`%LocalAppData%\DeskNotes\settings.json`

El archivo contiene las rutas de las notas abiertas y, para cada ventana, sus coordenadas, dimensiones y estado `Topmost`.

La implementación utiliza `System.Text.Json` y escribe primero un archivo temporal en el mismo directorio. Una vez serializado y cerrado correctamente, sustituye el archivo de configuración anterior. Las escrituras realizadas por la misma instancia del servicio se serializan para evitar que se pisen entre sí.

Al iniciar:
- Se ignoran rutas vacías, duplicadas o que ya no existan.
- Se intenta abrir cada nota guardada.
- Se restaura la geometría solo si es válida y conserva una parte suficiente de la ventana dentro del área virtual de los monitores.
- Si la configuración no se puede leer, se informa al usuario y DeskNotes inicia una sesión nueva.

## Alternativas consideradas

- **Registro de Windows:** nativo, pero menos sencillo de inspeccionar, respaldar o editar.
- **SQLite:** demasiado complejo para unas pocas preferencias y rutas; añadiría una dependencia y mantenimiento innecesarios.
- **JSON en AppData:** sencillo, legible, fácil de probar y suficiente para el alcance actual.

## Consecuencias

### Positivas
- Las preferencias sobreviven al cierre de la aplicación.
- No se modifica ni duplica el contenido de los documentos.
- El archivo puede inspeccionarse y respaldarse fácilmente.
- No se necesita una base de datos ni paquetes externos.

### Limitaciones
- Las rutas son locales a ese equipo y no sincronizan documentos entre dispositivos.
- Si un archivo se mueve o elimina, no se puede restaurar esa ventana.
- La geometría puede dejar de ser válida al cambiar la configuración de monitores; se valida antes de restaurar.
- La configuración no es un formato público ni una garantía de compatibilidad entre versiones.

## Revisión futura

Si la configuración crece o requiere migraciones, se añadirá una versión de esquema y una estrategia explícita de migración. La sincronización seguirá fuera del alcance de esta decisión.
