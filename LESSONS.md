# 📓 Lecciones Aprendidas - Gestor de Juegos

Este archivo registra fallos técnicos, soluciones complejas y "trucos" arquitectónicos descubiertos durante el desarrollo para evitar regresiones.

---

### 💾 1. Persistencia de Carátulas (Campos NotMapped)
*   **Problema**: Las carátulas principales (`Cover` y `CoverType`) en el modelo `Game` están marcadas como `[NotMapped]` en `AppDbContext` porque residen en `GestorCovers.db`. Si usas `context.SaveChanges()` sobre el contexto principal, las imágenes se pierden silenciosamente.
*   **Solución**: Usar siempre `GameService` para actualizar juegos que incluyan cambios en multimedia. El servicio gestiona la persistencia en ambas bases de datos.

### 🧵 2. Bloqueos de UI y Concurrencia
*   **Problema**: Las operaciones de E/S de disco (escaneo de carátulas) o consultas SQL pesadas congelan la interfaz de Avalonia.
*   **Solución**: 
    - Envolver lógica pesada en `Task.Run(() => { ... })`.
    - Usar `Dispatcher.UIThread.Post` para cualquier cambio que afecte a controles visuales (como barras de progreso).
    - **Lección Crítica**: Implementar siempre banderas de reentrada (ej. `_isSyncing`) para evitar que eventos rápidos (como cambiar de juego) disparen múltiples tareas asíncronas que compitan por el mismo recurso.

### 🔍 3. Motor de Matching Inteligente (Fix #18)
*   **Problema**: Juegos con regiones en el nombre (`Game (USA)`) no encontraban archivos simples (`Game.png`) o viceversa.
*   **Solución**: Implementar matching **bidireccional**. Normalizar ambos nombres (quitar no-alfanuméricos) y comprobar si uno empieza por el otro en cualquiera de las dos direcciones. Usar `BinarySearch` sobre una lista ordenada de llaves de archivos para mantener el rendimiento O(log N).

### 🏛️ 4. Nombres de Carpeta de Plataformas
*   **Problema**: El escáner fallaba si la carpeta se llamaba "SNES" pero la plataforma en la app era "Super Nintendo".
*   **Solución**: Usar la tabla `PlatformAlternateNames`. El escáner debe iterar sobre todos los alias posibles antes de dar una plataforma por "no encontrada".

### 🏗️ 5. Manejo de Archivos XAML.cs Gigantes (Clases Parciales)
*   **Problema**: `MainWindow.axaml.cs` creció por encima de las 6000 líneas, volviéndose inmanejable y propenso a errores de mezcla de lógica de negocio y efectos visuales.
*   **Solución**: Implementar el patrón de **Clases Parciales**. Mover toda la lógica de manipulación de UI (transformaciones, animaciones de ruedas, overlays de logros) a un archivo separado (ej: `MainWindow.UI.cs`).
*   **Lección**: Esto mantiene el archivo principal limpio para lógica de datos y servicios, mientras que el archivo `.UI.cs` encapsula la complejidad visual. El compilador los une automáticamente.

### 🧱 6. Modularización de Overlays y ScannerService
*   **Problema**: La lógica de escaneo y los manejadores de eventos de ventanas secundarias (overlays) abarrotaban `MainWindow.axaml.cs`.
*   **Solución**: 
    - Extraer cada panel complejo a un `UserControl` dedicado (`AchievementsView`, `ManagePlatformsView`).
    - Comunicación via eventos: El hijo dispara eventos (`RequestClose`, `DataChanged`) y el padre solo coordina la visibilidad y el refresco de datos.
    - **ScannerService**: Mover toda la lógica de infraestructura (procesamiento de archivos, XML, Regex) a un servicio puro de C#. Esto permite que `MainWindow` solo pida "escanear X" y reciba progreso, sin conocer los detalles del sistema de archivos.

### ☀️ 7. Cambio de Tema en Caliente (Hot-Toggle)
*   **Problema**: Cambiar de Dark a Light suele requerir reinicio si los colores están "hardcoded".
*   **Solución**: Usar `DynamicResource` en el XAML y definir un diccionario centralizado (`Colors.axaml`). Para el toggle, sobrescribir dinámicamente las claves del diccionario (`this.Resources["PanelBrush"] = ...`) en tiempo de ejecución. Esto permite cambios instantáneos sin recargar la ventana.

---
*Última actualización: 10 de junio de 2026*

### ⚠️ 8. Refactorización Masiva y Herramientas de Edición
*   **Problema**: Al intentar sobrescribir archivos de gran tamaño (>4000 líneas), las herramientas pueden fallar o realizar recortes accidentales si no se maneja con cuidado el contexto de las cadenas.
*   **Solución**:
    - **NUNCA** usar `write_file` para reemplazar el contenido completo de archivos gigantes; usar siempre `replace` de forma quirúrgica sobre bloques pequeños.
    - **Validación Continua**: Ejecutar `dotnet build` tras cada cambio atómico para detectar referencias huérfanas inmediatamente.
    - **Git como Red de Seguridad**: Realizar commits de "punto de control" antes de cambios estructurales profundos. Si ocurre un fallo masivo, `git checkout` es el mejor aliado.

### 🧩 9. Encapsulación de UI Compleja (GameDetailsView)
*   **Problema**: Mover controles que tienen lógica de actualización dinámica (ej. Visor 3D) rompe el enlace directo en `MainWindow`.
*   **Solución**:
    - Exponer métodos públicos en el `UserControl` (`UpdateDetails`, `SetCover`) para que el padre pueda inyectar datos.
    - El hijo debe gestionar su propia UI interna (ej. borrar listas, actualizar barras de progreso) para mantener la cohesión.
    - **Servicios Compartidos**: Inyectar instancias de servicios (`GameService`) en los métodos de actualización del componente para permitirle realizar consultas de datos locales de forma autónoma.

