# Hoja de ruta

**Proyecto:** DeskNotes  
**Estado:** MVP en desarrollo  
**Versión actual prevista:** 0.1.0

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

**Estado de implementación:** el flujo de creación, apertura, edición y guardado TXT/Markdown está implementado en `feature/note-window-mvp`; la persistencia/restauración de ventanas está en `feature/window-state-persistence`; «Guardar como» está en `feature/save-as-workflow`. Estas ramas siguen en PR y no forman parte de `main`. Las pruebas de almacenamiento y configuración pasan en CI; la separación testeable del guardado está implementada y la biblioteca persistente «Mis notas» está en desarrollo. Falta validar manualmente la interfaz y los escenarios de sesión en Windows.

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
- [ ] Gestionar errores de lectura y escritura.
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
- [ ] Verificar manualmente los criterios de aceptación del MVP.
- [x] Actualizar README y documentación.
- [ ] Preparar la primera versión 0.1.0.

**Resultado esperado:** flujo básico de notas fiable y comprobado. Las casillas de implementación no sustituyen la validación manual de los criterios de aceptación.

## Fase 2 — Experiencia de usuario (v0.2.x)

**Objetivo:** mejorar la comodidad y la presentación de las notas.

- [ ] Guardado automático configurable.
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

### Integración con el escritorio de Windows

Se estudiará si es viable mostrar notas directamente en la superficie del escritorio, detrás de los iconos. Esta integración requiere comportamiento específico de Windows y no debe condicionar el diseño del MVP.

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
