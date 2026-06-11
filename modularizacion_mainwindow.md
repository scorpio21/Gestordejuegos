# Plan de Refactorización Modular: MainWindow (Arquitectura 7.0)

## 🎯 Objetivo
Descomponer el archivo monolítico `MainWindow.axaml` (y su code-behind) en componentes modulares (`UserControls`) organizados por carpetas, cumpliendo con la regla de **300-400 líneas por archivo** y mejorando la mantenibilidad.

## 📅 Estado del Plan
- **Inicio**: 10 de junio de 2026
- **Finalización Fase 8**: 11 de junio de 2026
- **Progreso**: 100% COMPLETADO (Plan original y expansión de servicios).
- **Resultado Actual**: MainWindow reducida a 275 líneas.

---

## 🛠️ Fases del Proyecto

### ✅ Fase 1 a 7: Reconstrucción y Reorganización
- [x] Modularización de Paneles principales (TopBar, Sidebar, Library, Details, Dashboard, Background).
- [x] Organización de carpetas y Namespaces.
- [x] **Estado**: COMPLETADO.

### ✅ Fase 8: Herramientas de Importación y Exportación
- [x] Extraer `ExportOptionsView` a `Views/Overlays/`.
- [x] Modularizar Diálogos (`SimpleDialogView`, `DeleteConfirmView`).
- [x] Extraer Visor de Imágenes (`ImageViewerView`).
- [x] Crear `ImportService` para lógica de escaneo.
- [x] **Estado**: COMPLETADO.

---

## 📂 Estructura de Carpetas Final (v7.0)
```text
/GestorJuegos/Views
   /Windows   -> MainWindow, OpcionesWindow, ColorPickerDialog
   /Overlays  -> Achievements, EditGame, ManagePlatforms, FullStats, ExportOptions, SimpleDialog, DeleteConfirm, ImageViewer, ProgressOverlay
   /Panels    -> TopBar, Sidebar, Library, GameDetails, Dashboard, Background
   /Items     -> GameGridItem
```

---

## 🏆 Hito Alcanzado
Se ha logrado el desacoplamiento total de las herramientas de apoyo de `MainWindow`. La ventana principal ahora solo contiene la declaración de los componentes y la suscripción a sus eventos.
