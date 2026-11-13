# ADR-002: Los archivos del usuario son la fuente original del contenido

- **Estado:** aceptada
- **Fecha:** 2026-11-13
- **Decisores:** responsable del proyecto
- **Contexto:** DeskNotes debe permitir trabajar con documentos de texto normales.

## Contexto

La aplicación debe abrir, editar y mostrar documentos que el usuario ya tiene en su sistema. El usuario debe poder seguir accediendo a esos documentos con otros editores y herramientas.

Almacenar todo el contenido exclusivamente en una base de datos propia crearía dependencia de DeskNotes y dificultaría la interoperabilidad.

## Decisión

Los archivos TXT y MD seleccionados por el usuario serán la fuente original del contenido de las notas.

DeskNotes leerá el contenido desde el archivo y guardará los cambios en el archivo asociado. No se creará un formato propietario obligatorio ni se exigirá importar los documentos a una base de datos para poder editarlos.

La aplicación podrá almacenar por separado datos auxiliares, como preferencias, estado de ventanas o referencias a archivos recientes. Estos datos no sustituirán al contenido de los documentos originales.

## Alternativas consideradas

### Base de datos como almacenamiento principal

Ventaja:
- Facilita ciertas consultas y relaciones entre datos.

Inconvenientes:
- El contenido dejaría de estar disponible como documento normal sin exportación.
- Aumenta la dependencia de la aplicación.
- No encaja con el objetivo de editar archivos existentes directamente.

### Formato propietario por nota

Ventaja:
- Permite incluir metadatos y estructura personalizada.

Inconvenientes:
- Reduce la interoperabilidad con otros editores.
- No satisface la prioridad de trabajar con archivos de texto estándar.

### Archivos originales más configuración local auxiliar

Ventajas:
- Mantiene los documentos portables.
- Permite que otros programas los lean y editen.
- Permite almacenar preferencias de interfaz sin mezclar esos datos con el texto.

Inconveniente:
- Es necesario gestionar rutas que pueden cambiar y modificaciones realizadas desde otras aplicaciones.

Esta es la alternativa elegida.

## Consecuencias

Positivas:
- Los documentos siguen siendo archivos normales.
- El usuario conserva el control de sus archivos.
- La aplicación no necesita una base de datos para almacenar el contenido de las notas.
- Es posible abrir los documentos desde otros editores.

Responsabilidades técnicas:
- Gestionar errores de lectura y escritura.
- Definir la política de codificaciones.
- Detectar o gestionar modificaciones externas.
- Evitar sobrescribir silenciosamente cambios ajenos.
- Proteger los cambios pendientes del usuario.
- Definir cómo se comportan las notas nuevas que todavía no tienen archivo.

## Alcance

La decisión se aplica al contenido de las notas. No impide utilizar almacenamiento local auxiliar para preferencias, índices reconstruibles o metadatos de la aplicación, siempre que no se convierta en la única copia del contenido del usuario.

## Revisión

La decisión podrá revisarse si se introduce sincronización, colaboración o una funcionalidad que requiera almacenamiento adicional. Cualquier solución futura deberá preservar la posibilidad de acceder a los documentos originales y explicar claramente cómo se resuelven los conflictos.
