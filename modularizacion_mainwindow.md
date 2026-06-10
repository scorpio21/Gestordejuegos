# Plan de Refactorización Modular: MainWindow (Arquitectura 5.0)

## 🎯 Objetivo
Descomponer el archivo monolítico `MainWindow.axaml` (y su code-behind) en componentes modulares (`UserControls`) organizados por carpetas, cumpliendo con la regla de **< 300 líneas por archivo** y mejorando la mantenibilidad.

## 📅 Estado del Plan
- **Inicio**: 10 de junio de 2026
- **Progreso**: 45% completado
- **Siguiente Hito**: Fase 3 (Servicio de Lanzamiento)

---

## 🛠️ Fases del Proyecto

### ✅ Fase 1: Panel Superior (TopBarView)
- [x] Extraer XAML del menú horizontal y barra de búsqueda.
- [x] Crear `TopBarView.axaml.cs` con eventos para acciones (Search, Settings, ViewMode).
- [x] Integrar en `MainWindow`.
- [x] **Estado**: COMPLETADO.

### ✅ Fase 2: Detalles del Juego (GameDetailsView)
- [x] Extraer panel derecho de información del juego.
- [x] Modularizar visor 3D interactivo y carrusel de capturas.
- [x] Trasladar lógica de RetroAchievements al componente.
- [x] Implementar eventos `RequestLaunch`, `RequestEdit`, etc.
- [x] **Estado**: COMPLETADO y ESTABILIZADO.

### ⏳ Fase 3: Servicio de Lanzamiento (LauncherService)
- [ ] Desacoplar lógica de ejecución de emuladores de `MainWindow`.
- [ ] Implementar tracking de tiempo de juego asíncrono.
- [ ] Gestionar múltiples discos y argumentos de línea de comandos.
- [ ] **Estado**: PENDIENTE (Siguiente paso).

### ⏳ Fase 4: Barra Lateral y Filtros (SidebarView)
- [ ] Mover el `TreeView` de plataformas y los filtros de vista.
- [ ] Emitir evento `PlatformChanged` para actualizar la lista de juegos.
- [ ] **Estado**: PENDIENTE.

### ⏳ Fase 5: Vistas de Juegos (GamesGridView / GamesListView)
- [ ] Separar los contenedores de la lista de juegos (Muro vs Lista).
- [ ] Optimizar el renderizado y la lógica de selección cruzada.
- [ ] **Estado**: PENDIENTE.

### ⏳ Fase 6: Dashboard y Sistema de Fondos
- [ ] Modularizar `DashboardView` (Pantalla de bienvenida).
- [ ] Extraer `BackgroundView` para la gestión de fondos dinámicos y vignettes.
- [ ] **Estado**: PENDIENTE.

---

## 🛡️ Protocolo de Edición Segura

Para evitar regresiones y errores de compilación inmanejables durante la modularización, se aplicarán las siguientes reglas:

1.  **Respaldo Preventivo (.tmp)**: Antes de realizar un movimiento masivo de código en archivos críticos (`MainWindow.axaml.cs`), se creará una copia de seguridad con extensión `.tmp`.
    - *Ejemplo*: `MainWindow.axaml.cs.tmp`.
    - Esto permite rescatar bloques de código si la herramienta de edición realiza un recorte accidental.

2.  **Limpieza Incremental de Referencias**: Al mover un bloque de código o control al nuevo componente, **SE DEBEN BORRAR** inmediatamente las referencias antiguas en el archivo de origen.
    - No esperar al final de la fase para limpiar.
    - Esto reduce el número de errores en cada compilación intermedia y facilita la depuración.

3.  **Validación Atómica**: Compilar (`dotnet build`) tras cada movimiento de función o control para confirmar que la comunicación entre el nuevo componente y el padre es correcta.

---

## 📂 Estructura de Carpetas Final
```text
/GestorJuegos
   /Views
      MainWindow.axaml (Contenedor principal ligero)
      /Panels
         TopBarView.axaml       (Menús, Búsqueda, Acciones rápidas)
         SidebarView.axaml      (Árbol de plataformas y filtros)
         GamesGridView.axaml    (El muro de carátulas)
         GamesListView.axaml    (La lista compacta)
         GameDetailsView.axaml  (El panel derecho con banners e info)
         BackgroundView.axaml   (Gestión de fondos dinámicos)
         DashboardView.axaml    (Pantalla de bienvenida)
```

---

## ⚠️ Notas de Estabilización
- Durante la Fase 2, se detectó una reducción accidental masiva de `MainWindow.axaml.cs`. Se restauró vía Git y se procedió con reemplazos quirúrgicos siguiendo el protocolo .tmp.
- **Sugerencia del Usuario**: La limpieza inmediata de referencias antiguas es vital para no saturar la lista de errores del compilador.
- Se debe asegurar que `GameService` proporcione todos los métodos de imagen necesarios para los nuevos componentes.
