# Hoja de ruta

**Proyecto:** DeskNotes  
**Estado:** Alpha 0.1.0 publicada como versión preliminar  
**Versión actual:** 0.1.0-alpha

Esta hoja de ruta describe el orden propuesto de desarrollo. Las fases son objetivos, no fechas comprometidas. Se podrán ajustar cuando aparezcan dificultades técnicas o cambien las prioridades.

## Fase 0 — Descubrimiento y preparación

**Objetivo:** definir el producto y preparar una base técnica y documental coherente.

- [x] Definir la idea principal de DeskNotes.
- [x] Elegir C#, .NET 10 y WPF.
- [x] Crear la solución y los proyectos iniciales App, Core e Infrastructure.
- [x] Configurar el repositorio Git y GitHub.
- [x] Completar y revisar la documentación del producto.
- [x] Definir las primeras tareas de desarrollo.
- [x] Definir la estrategia inicial de pruebas.

**Resultado esperado:** documentación inicial, arquitectura acordada y repositorio preparado para el desarrollo incremental.

## Fase 1 — MVP (v0.1.0)

**Objetivo:** disponer de una aplicación útil para crear, abrir y editar notas en ventanas flotantes.

**Estado de implementación:** completado e integrado en `main` mediante el PR #8. La Alpha 0.1.0 incorpora el flujo de notas TXT/Markdown, «Guardar como», biblioteca «Mis notas», persistencia de ventanas y widgets flotantes. La compilación, las pruebas automatizadas y la validación manual de los flujos principales se han completado.

### Gestión de notas y archivos

- [x] Crear una nota nueva.
- [x] Abrir archivos TXT.
- [x] Abrir archivos MD como texto editable.
- [x] Editar el contenido.
- [x] Guardar en el archivo original.
- [x] Guardar una nota en otra ubicación mediante «Guardar como».
- [x] Mantener una biblioteca de archivos guardados independiente de las ventanas abiertas.
- [x] Volver a abrir una nota desde «Mis notas».
- [x] Quitar una entrada de la biblioteca sin eliminar el archivo original.
- [ ] Mejorar la gestión y presentación de errores de lectura y escritura.
- [x] Avisar antes de descartar cambios sin guardar.

### Ventanas

- [x] Mostrar cada nota en una ventana independiente.
- [x] Permitir mover las ventanas.
- [x] Permitir redimensionarlas.
- [x] Activar y desactivar la opción de mantener encima.
- [x] Recordar posición y tamaño.
- [x] Restaurar las ventanas al iniciar la aplicación.

### Calidad

- [x] Incorporar pruebas automatizadas para almacenamiento, configuración y lógica de guardado.
- [x] Verificar manualmente los criterios principales de aceptación del MVP en Windows.
- [x] Actualizar README, changelog y hoja de ruta.
- [x] Preparar y publicar la primera versión preliminar 0.1.0-alpha.

**Resultado esperado:** flujo básico de notas fiable y comprobado. La edición directa dentro del widget queda fuera del alcance de esta Alpha; el widget permite abrir el editor mediante doble clic en la parte superior o el botón «Editar».

## Fase 2 — Experiencia de usuario (v0.2.x)

**Objetivo:** mejorar la comodidad y la presentación de las notas.

- [ ] Guardado automático configurable.
- [x] Widgets flotantes de escritorio con título y vista previa del contenido.
- [x] Mover, redimensionar y restaurar widgets entre sesiones.
- [x] Abrir una nota en el editor desde su widget.
- [ ] Editar el contenido directamente dentro del widget.
- [ ] Integrar widgets directamente en la capa del escritorio detrás de los iconos de Windows.
- [ ] Transparencia configurable.
- [ ] Colores y temas.
- [ ] Tipografías y tamaños de texto.
- [ ] Vista previa de Markdown.
- [ ] Menú contextual.
- [ ] Mejoras de gestión de múltiples notas.

**Resultado esperado:** una experiencia de uso más flexible y agradable.

## Fase 3 — Productividad (v0.3.x)

**Objetivo:** facilitar la organización y el acceso a la información.

- [ ] Listas de tareas interactivas.
- [ ] Categorías y etiquetas.
- [ ] Búsqueda de notas.
- [ ] Plantillas.
- [ ] Atajos de teclado.
- [ ] Inicio automático con Windows.
- [ ] Detección y gestión de modificaciones externas de archivos.

**Resultado esperado:** herramienta de notas más completa y adecuada para un uso diario.

## Fase 4 — Preparación como producto (v1.0.0)

**Objetivo:** preparar una versión estable y distribuible.

- [ ] Evaluar un sistema de complementos.
- [ ] Evaluar sincronización opcional.
- [ ] Incorporar copias de seguridad.
- [ ] Importación y exportación.
- [ ] Crear instalador.
- [ ] Diseñar el mecanismo de actualización.
- [ ] Optimizar rendimiento y consumo de recursos.
- [ ] Completar la guía de usuario.
- [ ] Definir política de versiones y publicación.
- [ ] Ejecutar pruebas de regresión antes de publicar.

**Resultado esperado:** primera versión estable, documentada y distribuible.

## Líneas de investigación futuras

### Integración profunda con el escritorio de Windows

La primera implementación usa ventanas WPF compactas, sin borde ni entrada en la barra de tareas, que se pueden mover, redimensionar y mantener encima opcionalmente. Como evolución independiente se estudiará la integración en la superficie del escritorio detrás de los iconos; requiere comportamiento específico de Windows y no se considera necesaria para la primera versión de los widgets.

### Sincronización

Se estudiará únicamente después de consolidar el almacenamiento local y resolver correctamente los conflictos de edición. La sincronización no debe poner en riesgo los archivos originales.

## Política de priorización

1. Fiabilidad y protección de los datos.
2. Funcionalidad esencial del MVP.
3. Facilidad de uso.
4. Personalización.
5. Funciones avanzadas.

No se añadirá una funcionalidad nueva si compromete la estabilidad de las ya implementadas.

## Definición de terminado

Una tarea se considera terminada cuando:

- Cumple su objetivo y criterios de aceptación.
- El proyecto compila correctamente.
- Las pruebas relevantes pasan.
- Se ha probado manualmente cuando el comportamiento lo requiere.
- Los errores y casos límite relevantes están controlados.
- La documentación afectada está actualizada.
- Los cambios han sido revisados y versionados.
