# Requisitos del producto

**Proyecto:** DeskNotes  
**Estado:** borrador inicial aprobado como base de trabajo  
**Versión objetivo del MVP:** 0.1.0

## 1. Propósito

DeskNotes es una aplicación de escritorio para Windows que permite abrir, crear y editar documentos de texto y mostrarlos en ventanas flotantes independientes. Su propósito es mantener información útil visible en el escritorio sin obligar al usuario a abrir un editor de texto completo cada vez.

Los documentos deben seguir siendo archivos normales. La aplicación no debe convertirlos en un formato propietario ni depender de una base de datos como único almacenamiento del contenido.

## 2. Usuarios y necesidades

El usuario principal es una persona que desea tener a la vista información breve o documentos de consulta frecuente, por ejemplo:

- Una lista de tareas.
- Ideas o recordatorios.
- Notas de un proyecto.
- Documentación en Markdown.
- Anotaciones personales.

## 3. Alcance del MVP (v0.1.0)

El MVP debe proporcionar un flujo completo y fiable para crear, abrir, editar y guardar notas.

### 3.1 Requisitos funcionales

| ID | Requisito | Prioridad |
|---|---|---|
| FR-001 | La aplicación debe iniciarse en Windows sin errores no controlados. | Obligatoria |
| FR-002 | El usuario debe poder crear una nota nueva. | Obligatoria |
| FR-003 | El usuario debe poder abrir un archivo de texto compatible desde el sistema de archivos. | Obligatoria |
| FR-004 | La primera versión debe admitir archivos TXT y MD como texto editable. | Obligatoria |
| FR-005 | El usuario debe poder editar el contenido de una nota. | Obligatoria |
| FR-006 | El usuario debe poder guardar el contenido en el archivo asociado. | Obligatoria |
| FR-007 | Cada nota abierta debe mostrarse en una ventana independiente. | Obligatoria |
| FR-008 | El usuario debe poder mover y redimensionar cada ventana de nota. | Obligatoria |
| FR-009 | El usuario debe poder activar y desactivar que una nota permanezca siempre encima de otras ventanas. | Obligatoria |
| FR-010 | La aplicación debe recordar la posición y el tamaño de las ventanas entre ejecuciones. | Obligatoria |
| FR-011 | La aplicación debe informar al usuario cuando una operación de archivo no pueda completarse. | Obligatoria |
| FR-012 | La aplicación debe mantener una biblioteca persistente de archivos guardados, independiente de las ventanas abiertas. | Obligatoria |
| FR-013 | El usuario debe poder volver a abrir un archivo desde la biblioteca o quitar su entrada sin borrar el archivo original. | Obligatoria |

### 3.2 Comportamientos que deben definirse durante la implementación

Antes de dar por terminadas las funciones relacionadas, se concretarán y probarán estos casos:

- Qué ocurre cuando se cierra una nota con cambios sin guardar.
- Qué ocurre si el archivo original se elimina, se mueve o deja de estar accesible.
- Qué ocurre si otro programa modifica el archivo mientras DeskNotes lo tiene abierto.
- Qué codificaciones de texto se detectan y cuáles se utilizan al guardar.
- Qué ocurre si la ruta guardada para una nota deja de ser válida.
- Cómo se comporta la aplicación cuando una ventana queda fuera del área visible de los monitores conectados.

No se debe descartar silenciosamente ningún cambio del usuario.

## 4. Formatos de archivo

### Incluidos en el MVP

- `.txt`: texto sin formato.
- `.md`: Markdown tratado como texto editable.

En el MVP, un archivo Markdown no necesita mostrarse renderizado. La vista previa interpretada de Markdown pertenece a una fase posterior.

### Fuera del MVP

No se promete compatibilidad con formatos ofimáticos como DOCX, PDF u hojas de cálculo. Se evaluarán por separado si el producto lo necesita.

## 5. Requisitos no funcionales

| ID | Requisito |
|---|---|
| NFR-001 | La aplicación debe utilizar C# y .NET 10. |
| NFR-002 | La interfaz de escritorio debe estar implementada con WPF y XAML. |
| NFR-003 | La aplicación debe funcionar en Windows; el objetivo inicial de producto es Windows 11. La matriz exacta de versiones compatibles se confirmará antes de publicar. |
| NFR-004 | El contenido debe permanecer en archivos normales accesibles para otras aplicaciones. |
| NFR-005 | La aplicación debe separar interfaz, lógica de dominio y acceso al sistema de archivos. |
| NFR-006 | Los errores de lectura, escritura y acceso deben gestionarse y comunicarse de forma comprensible. |
| NFR-007 | Las operaciones de guardado deben minimizar el riesgo de pérdida o corrupción de datos. |
| NFR-008 | La aplicación debe mantener un consumo de recursos razonable para una utilidad de escritorio. |
| NFR-009 | El código debe poder compilarse y probarse mediante el SDK de .NET y herramientas automatizables. |
| NFR-010 | La documentación debe reflejar las funcionalidades realmente implementadas. |

## 6. Fuera de alcance del MVP

Las siguientes funciones no son necesarias para publicar la primera versión funcional:

- Vista previa visual de Markdown.
- Guardado automático.
- Transparencia configurable.
- Temas, colores y tipografías personalizables.
- Listas de tareas interactivas.
- Etiquetas, categorías y búsqueda.
- Plantillas.
- Atajos globales de teclado.
- Inicio automático con Windows.
- Sincronización entre dispositivos.
- Sistema de complementos.
- Instalador y actualizaciones automáticas.
- Integración de notas directamente en la capa del escritorio detrás de los iconos.

Estas funciones se valorarán en la hoja de ruta y no deben retrasar el MVP.

## 7. Criterios de aceptación del MVP

El MVP podrá considerarse funcional cuando se compruebe que:

1. La solución compila sin errores.
2. La aplicación se inicia correctamente en un entorno Windows compatible.
3. Se puede crear una nota y escribir contenido.
4. Se puede abrir un archivo TXT y editarlo.
5. Se puede abrir un archivo MD y editarlo como texto.
6. Al guardar, el contenido queda escrito en el archivo correspondiente.
7. Se pueden abrir varias notas en ventanas independientes.
8. Las ventanas se pueden mover y redimensionar.
9. La opción de mantener encima se puede activar y desactivar.
10. La posición y el tamaño de las notas se restauran correctamente después de cerrar y volver a abrir la aplicación.
11. Una nota guardada permanece en «Mis notas» después de cerrar su ventana y reiniciar DeskNotes.
12. Quitar una entrada de «Mis notas» no elimina el archivo original.
11. Los errores de acceso a archivos se comunican sin provocar un cierre inesperado.
12. Los cambios no guardados no se descartan sin una decisión explícita del usuario.

## 8. Suposiciones y decisiones pendientes

- La primera plataforma objetivo es Windows 11.
- WPF se mantiene como tecnología de interfaz.
- Los archivos originales son la fuente de verdad del contenido.
- La configuración de ventanas y preferencias puede almacenarse separadamente del contenido de las notas.
- La estrategia exacta de detección de modificaciones externas, codificaciones y guardado seguro debe diseñarse antes de implementar el servicio de archivos.
- La integración de notas directamente en el escritorio (detrás de los iconos) no forma parte del MVP.

## 9. Trazabilidad

Cada requisito funcional deberá asociarse a una tarea de desarrollo y a una o más pruebas manuales o automatizadas. Un requisito no se marcará como completado únicamente porque exista código: debe verificarse su comportamiento.
