# Investigación técnica: widgets en la capa del escritorio

**Fecha:** 2026-11-13  
**Objetivo inicial:** Windows 11 25H2 x64  
**Estado:** investigación completada inicialmente; prototipos nativos pendientes de validación en máquina interactiva.

## 1. Estado del repositorio

- \`main\` permanece como rama estable de desarrollo.
- PR #13 (\`feature/desktop-wallpaper-layer\`) sigue abierto como Draft y no está integrado.
- PR #13 parte de \`e8c08db65912a234d9822cf4eae576b4246dce47\` y su HEAD auditado es \`911e2dbf685010424b353aa53c055144d35b2b4f\`.
- La rama experimental contiene 13 commits: primero intentó gestionar ventanas top-level y el orden Z; después cambió a reparentar HWND WPF directamente a Progman.
- Esta investigación no modifica ni fusiona PR #13.

## 2. Arquitectura actual auditada

\`MainWindow\` crea y administra \`NoteWidgetWindow\`, restaura widgets desde \`AppSettings.NoteWidgets\`, y persiste geometría y estado. La preferencia global \`DesktopLayerEnabled\` se guarda mediante \`JsonAppSettingsService\`.

\`NoteWidgetWindow\` es una ventana WPF independiente con:
- \`WindowStyle=None\`
- \`AllowsTransparency=True\`
- \`Background=Transparent\`
- \`ShowInTaskbar=False\`
- controles WPF interactivos, \`DragMove()\` y redimensionamiento nativo.

El servicio experimental cambia estilos, reparenta el HWND existente a Progman y modifica el orden Z. No existe todavía una prueba de integración que valide composición DWM, entrada real o foco.

## 3. Hallazgos confirmados

### 3.1 Windows / Win32

1. \`SetParent\` no modifica automáticamente \`WS_CHILD\` ni \`WS_POPUP\`; el llamador debe hacerlo. Microsoft también exige sincronizar UI state y advierte sobre diferencias de DPI awareness entre parent y child.
2. Desde Windows 8, \`WS_EX_LAYERED\` puede usarse con ventanas child y top-level. Por tanto, afirmar que Windows prohíbe universalmente una ventana layered hija sería incorrecto.
3. En Windows 10 1703 y posteriores, una relación HWND con contextos DPI incompatibles puede provocar un restablecimiento forzado de DPI awareness del proceso hijo en operaciones cross-process. Es un riesgo de proceso completo, no solo de coordenadas.
4. \`SetWindowPos\` modifica tamaño, posición y orden Z. El orden de una ventana child está agrupado con su parent; no es equivalente a gestionar una ventana top-level.
5. La jerarquía Progman / SHELLDLL_DefView / WorkerW y el mensaje \`0x052C\` son detalles internos del shell, no un contrato público estable de Windows.

### 3.2 WPF

1. \`AllowsTransparency=True\` permite transparencia per-pixel en una WPF Window.
2. \`HwndSource\` es la vía oficial para alojar contenido WPF en una ventana Win32.
3. La documentación de \`HwndSourceParameters.UsesPerPixelOpacity\` especifica que esa propiedad solo se aplica a ventanas top-level.
4. El código fuente de WPF condiciona la adición de \`WS_EX_LAYERED\` a que el HWND no sea \`WS_CHILD\` cuando usa \`UsesPerPixelOpacity\`. Por ello, cambiar directamente de Window a HwndSource child no conserva automáticamente la transparencia per-pixel.
5. La compatibilidad Win32 de layered child windows no prueba por sí misma que cualquier ciclo de vida de \`Window\` WPF sea seguro después de cambiar su parent.

### 3.3 Defectos / carencias del PR #13

- El servicio no valida \`WS_EX_NOREDIRECTIONBITMAP\` para confirmar la ruta moderna; usa un umbral de build y una comprobación parcial de jerarquía.
- Encuentra el primer \`WorkerW\` hijo de Progman, pero no demuestra que sea la superficie de wallpaper correcta.
- Usa \`SWP_NOMOVE | SWP_NOSIZE\` tras cambiar el parent; no realiza una conversión explícita de coordenadas al espacio cliente del nuevo parent.
- No sincroniza UI state tras cambiar parent.
- No registra ni verifica contextos DPI de Progman y del HWND WPF.
- Reordena el WorkerW durante cada attach. Con más de un widget, la secuencia puede intercalar el WorkerW entre widgets, dejando widgets anteriores detrás de la superficie del wallpaper.
- El timer de dos segundos no recupera Explorer: solo detecta algunos HWND inválidos y retorna.
- La restauración no tiene una estrategia completa de geometría/monitor/espacio de coordenadas.
- \`DesktopLayerStatePolicy\` tiene pruebas puras, pero no demuestra comportamiento nativo y no está integrada en un controlador efectivo de foco/recuperación.
- El servicio no demuestra composición, input, foco, múltiples monitores ni reinicio de Explorer.

## 3.4 Hallazgos de Lively y estrategia raised desktop

Se inspeccionó Lively en el commit [25a0a5f4ed1988ff42117d3a17feb321062237fe](https://github.com/lively-community/lively/blob/25a0a5f4ed1988ff42117d3a17feb321062237fe/src/Lively/Lively/Core/WinDesktopCore.cs). Su implementación distingue dos topologías:

- **Legacy:** aloja el wallpaper bajo un WorkerW seleccionado mediante la enumeración del shell.
- **Raised desktop / layered shell view:** detecta WS_EX_NOREDIRECTIONBITMAP en Progman, busca SHELLDLL_DefView y un WorkerW hijo directo de Progman. Inserta su ventana layered como hijo de Progman, inmediatamente detrás de SHELLDLL_DefView, y fuerza WorkerW al fondo del orden Z.

Lively aplica WS_CHILD y WS_EX_LAYERED antes del attach. Para su renderer DirectX utiliza SetLayeredWindowAttributes(alpha=255); esto es distinto de UpdateLayeredWindow con alpha per-pixel. DeskNotes compara ambos caminos en HWNDs recreados, no alterna los mecanismos sobre la misma ventana.

La detección mediante WS_EX_NOREDIRECTIONBITMAP es más sólida que inferir la topología únicamente por la versión de Windows. El mensaje 0x052C y la jerarquía concreta siguen siendo detalles internos del shell, no contratos públicos estables.

El requisito Windows 10 1903+ publicado por Lively corresponde a su distribución/instalador y dependencias; no demuestra que SetParent, WorkerW o layered child windows aparecieran en esa versión. Microsoft documenta layered child windows desde Windows 8, con la condición de declarar compatibilidad con Windows 8+ en el manifiesto. Esto tampoco garantiza por sí solo la compatibilidad de WPF.

El NativeProbe incorpora ahora app.manifest con compatibilidad declarada para Windows 8, 8.1 y Windows 10/11, y el proyecto lo incrusta mediante ApplicationManifest. Referencias: [Microsoft — Window Features](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features) y [Microsoft — Application manifests](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests).

Fuentes:
- [Lively — WinDesktopCore.cs, commit inspeccionado](https://github.com/lively-community/lively/blob/25a0a5f4ed1988ff42117d3a17feb321062237fe/src/Lively/Lively/Core/WinDesktopCore.cs)
- [Lively — DesktopUtil.cs](https://github.com/lively-community/lively/blob/25a0a5f4ed1988ff42117d3a17feb321062237fe/src/Lively/Lively.Common/Helpers/Shell/DesktopUtil.cs)
- [Lively — WindowUtil.cs](https://github.com/lively-community/lively/blob/25a0a5f4ed1988ff42117d3a17feb321062237fe/src/Lively/Lively.Common/Helpers/WindowUtil.cs)
- [Lively — Getting Started / requisitos de Windows](https://github.com/lively-community/lively/wiki/Getting-Started)

## 4. Comparación de alternativas

| Alternativa | Viabilidad inicial | Riesgo / limitación principal |
|---|---|---|
| A. WPF top-level + Z-order | Baja para la composición moderna | No inserta una ventana top-level dentro del grupo Z de hijos de Progman |
| B. Reparentar Window WPF existente | Posible, aún no demostrada | DPI cross-process, coordenadas, ciclo de vida WPF y recuperación |
| C. Host Win32 + HwndSource child | Parcial | El API documenta per-pixel opacity solo para top-level; el código WPF elimina WS_EX_LAYERED para WS_CHILD en ese camino |
| D. Host nativo + superficie layered propia | Alta para probar composición | No proporciona por sí solo controles WPF ni input semántico |
| E. Proceso WPF aislado + host nativo | Hipótesis prometedora | IPC, DPI del proceso anfitrión, reinicio del shell y ciclo de vida multiproceso |

No se selecciona todavía una arquitectura de producción. La alternativa E no debe adoptarse hasta probar que el proceso anfitrión puede igualar el contexto DPI de Explorer antes de crear HWNDs y que las ventanas WPF layered hijas conservan renderizado e input.

## 5. Estrategia de experimentación

### Prototipo 1 — superficie nativa

Este repositorio añade \`prototypes/DesktopLayer.NativeProbe\`, una aplicación WinForms de diagnóstico aislada que:

- consulta Progman, SHELLDLL_DefView, WorkerW y los estilos relevantes;
- consulta el DPI awareness context de Progman y el hilo del proceso;
- intenta igualar el contexto DPI antes de crear ventanas;
- crea una ventana child \`WS_EX_LAYERED\` directamente bajo Progman;
- inserta la superficie inmediatamente detrás de SHELLDLL_DefView;
- mantiene el WorkerW hijo directo de Progman al fondo del orden Z, siguiendo el patrón raised desktop de Lively;
- permite ejecutar dos variantes independientes: \`UpdateLayeredWindow(ULW_ALPHA)\` con alpha per-pixel y \`SetLayeredWindowAttributes(alpha=255)\` con pintura GDI normal;
- registra HWNDs, parent, estilos, rectángulos, Z-order, arquitectura y errores Win32.

La prueba no cambia archivos de DeskNotes ni la configuración del usuario.

**Criterios de éxito:**
- panel visible encima del wallpaper;
- iconos visibles y por delante;
- iconos siguen siendo seleccionables;
- aplicaciones normales cubren el panel;
- el panel no activa ni roba el foreground al crearse;
- parent y relación Z verificables en el log.

**Criterios de fallo:**
- no se encuentra la jerarquía moderna;
- contexto DPI no igualable;
- CreateWindowEx / SetWindowPos / UpdateLayeredWindow falla;
- el panel queda detrás del wallpaper o por delante de los iconos;
- el panel es invisible.

Ejecuciones:
- Modo per-pixel (predeterminado): `dotnet run --project prototypes/DesktopLayer.NativeProbe/DesktopLayer.NativeProbe.csproj -c Release`
- Modo alpha constante Lively: `dotnet run --project prototypes/DesktopLayer.NativeProbe/DesktopLayer.NativeProbe.csproj -c Release -- --constant-alpha`

En cada ejecución se debe verificar que el widget es hijo de Progman, que queda inmediatamente detrás de SHELLDLL_DefView y que GetWindow(WorkerW, GW_HWNDNEXT) devuelve NULL (WorkerW al final del orden Z).

No se considera probado hasta ejecutar ambas variantes en una sesión interactiva de Windows 11 25H2.

### Prototipo 2 — WPF

Solo tras aprobar el prototipo 1:
- sustituir el dibujo nativo por una WPF Window mínima con transparencia;
- comprobar botones, TextBox, foco, teclado, arrastre y resize;
- registrar el contexto DPI y los mensajes de ventana;
- comparar Window reparentada frente a WPF alojado por HwndSource.

### Prototipo 3 — varios widgets

Solo tras aprobar el prototipo 2:
- varias superficies simultáneas;
- orden Z estable;
- foco independiente;
- movimiento, resize y destrucción;
- monitor virtual y mixed DPI.

### Prototipo 4 — integración

Solo después de aprobar los tres prototipos anteriores. Mantener el modo standalone como fallback y no modificar el modelo de notas ni su persistencia sin necesidad.

## 6. Evidencia y referencias

### Microsoft Learn
- [SetParent](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setparent)
- [High DPI desktop application development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
- [Window Features — Z-order and layered windows](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features)
- [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow)
- [SetFocus](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setfocus)
- [AttachThreadInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-attachthreadinput)
- [WPF and Win32 interoperation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-and-win32-interoperation)
- [HwndSourceParameters (.NET Desktop 10 API)](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndsourceparameters?view=windowsdesktop-10.0)
- [HwndSource (.NET Desktop 10 API)](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndsource?view=windowsdesktop-10.0)
- [WPF HwndSourceParameters source (inspected commit)](https://github.com/dotnet/wpf/blob/e7acf467c05e51901b17684b1f2c330e7dc5fc4d/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSourceParameters.cs)
- [WPF HwndSource source (inspected commit)](https://github.com/dotnet/wpf/blob/e7acf467c05e51901b17684b1f2c330e7dc5fc4d/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs)

### Código fuente externo inspeccionado
- [Ivy Windows Wallpaper — DesktopInterop.cs](https://github.com/Ivy-Interactive/Ivy-Windows-Wallpaper/blob/main/src/Ivy.Windows.Wallpaper/DesktopInterop.cs)
- [Ivy Windows Wallpaper — WallpaperForm.cs](https://github.com/Ivy-Interactive/Ivy-Windows-Wallpaper/blob/main/src/Ivy.Windows.Wallpaper/WallpaperForm.cs)
- [Lively — SetupDesktop.cs](https://github.com/lively-community/lively/blob/master/src/livelywpf/livelywpf/wp_lib/SetupDesktop.cs)
- [Rainmeter — Skin.cpp](https://github.com/rainmeter/rainmeter/blob/master/Library/Skin.cpp)
- [Rainmeter — Skin.h](https://github.com/rainmeter/rainmeter/blob/master/Library/Skin.h)

Ivy usa una superficie WinForms/WebView2 y \`WS_EX_TRANSPARENT\`; su código no valida widgets WPF interactivos. Lively aporta referencias de ciclo de vida de wallpaper, no una demostración de hosting WPF interactivo. Rainmeter aporta una referencia de gestión de widgets, DPI y orden Z, pero no valida la jerarquía moderna de Progman para DeskNotes.

## 7. Estado de validación

- Auditoría de main y PR #13: completada inicialmente.
- Investigación documental Win32 / WPF: completada inicialmente.
- Comparación de alternativas: completada inicialmente.
- Prototipo nativo: variantes A/B implementadas; compilación CI y ejecución interactiva pendientes.
- Prototipo WPF: bloqueado hasta el resultado del prototipo 1.
- Integración DeskNotes: no iniciada.
- Windows 10: fuera del primer objetivo.

**No declarar la función terminada ni fusionar PR #13 hasta completar las pruebas manuales en Windows 11 25H2.**
