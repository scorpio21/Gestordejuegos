# 🤖 Guía de Agentes - Proyecto: Gestor de Juegos

Este documento es la referencia técnica definitiva para cualquier IA o desarrollador que trabaje en este repositorio. Contiene la arquitectura, reglas de oro y convenciones críticas que **deben** respetarse para mantener la integridad del sistema.

---

## 🏛️ 1. Arquitectura de Datos (Dual DB)
El proyecto utiliza un sistema de **Base de Datos Dual** para optimizar el rendimiento y evitar la fragmentación de SQLite.

1.  **`GestorJuegos.db` (Principal)**:
    *   **Propósito**: Metadatos de juegos, plataformas, categorías, configuraciones y rutas.
    *   **Regla Crítica**: La tabla `Games` tiene campos como `Cover` y `CoverType` marcados como `[NotMapped]`. **NUNCA** intentes guardarlos usando el contexto de esta base de datos; se perderán.
2.  **`GestorCovers.db` (Multimedia)**:
    *   **Propósito**: Almacenamiento binario (BLOB) de imágenes.
    *   **Tablas**: `Covers` (Carátula principal) e `Images` (Extras: Snaps, Logos, Fanarts).
    *   **Acceso**: Siempre a través de `CoversDbContext` o, preferiblemente, mediante `GameService`.

---

## 🛠️ 2. Estándares de Codificación
*   **Idioma**: El código (nombres de funciones, variables), comentarios y UI deben estar en **español**.
*   **UI Responsiva**: Todas las operaciones pesadas (Escaneos, Carga de Plataformas, Importaciones) **DEBEN** ser asíncronas (`async/await`) y usar `Task.Run` para no bloquear el hilo principal (UI Thread).
*   **Seguridad de Hilos**: Las actualizaciones de la interfaz desde hilos secundarios se realizan siempre con `Avalonia.Threading.Dispatcher.UIThread.Post`.
*   **Versión de Framework**: .NET 9 con Avalonia UI.

---

## 🎮 3. Lógica de Multimedia y Escaneo
El motor de escaneo masivo (v2.2+) sigue estas reglas estrictas:

*   **Detección de Plataformas**: No busques solo por el nombre exacto. Consulta siempre `PlatformAlternateNames` para vincular carpetas como "SNES" a "Super Nintendo".
*   **Matching de Juegos (Bidireccional)**:
    1.  Elimina caracteres no alfanuméricos y convierte a minúsculas para la comparación.
    2.  Coincidencia Exacta.
    3.  Coincidencia por Prefijo: `archivo.StartsWith(juego)` (para juegos con región en el archivo).
    4.  Coincidencia Inversa: `juego.StartsWith(archivo)` (para juegos con región en la DB pero no en el archivo).
*   **Jerarquía de Arte**: Al asignar la carátula principal, prioriza `Box_3D` sobre `Box` (2D).

---

## 🎨 4. Sistema de Temas Dinámico
Los temas son 100% externos y residen en `/Themes/[NombreDelTema]`.

*   **`theme.json`**: Define los colores (`AccentBrush`, `PanelBrush`, etc.) y las fuentes.
*   **MainFont vs HeaderFont**: Los temas pueden inyectar sus propias fuentes `.ttf` o `.otf`. Siempre valida la fuente con `GlyphTypeface` antes de aplicarla para evitar crashes.
*   **Capas de Fondo**:
    1.  `Background_Image` (Fondo del tema).
    2.  `OverlayImage` (Texturas/Viñetas).
    3.  `Fanart` (Imagen específica del juego).
    4.  `ReadabilityTint` (Capa de color para legibilidad del texto).

---

## 🚨 5. Reglas de Oro para Agentes (Checklist)
1.  **Persistencia**: Si actualizas la carátula de un juego, usa `_gameService.UpdateGamesBatch` o `UpdateGame`. No uses `context.SaveChanges()` en la DB principal para el campo `Cover`.
2.  **Surgical Edits**: Prefiere el uso de la herramienta `replace` para ediciones quirúrgicas. Evita reescribir archivos de más de 500 líneas por completo.
3.  **Búsqueda de Funciones**: Antes de crear una nueva lógica, usa `grep_search` para ver si ya existe en `GameService` o `ImportService`. **Evita la duplicación**.
4.  **Notificaciones**: Usa los Overlays de la app para mensajes al usuario en lugar de `MessageBox` estándar cuando sea posible, para mantener la estética.
5.  **Git**: Siempre actualiza el `README.md` y `MEMORY.md` tras cambios significativos.

---

## 📂 6. Estructura de Proyectos
*   **`GestorJuegos/`**: Aplicación principal.
*   **`CreadorTemas/`**: Herramienta independiente para diseñadores.
*   **`GestorJuegos/Models/`**: Definición de la verdad de los datos.
*   **`GestorJuegos/Services/`**: Lógica de negocio (Donde ocurre la magia).

---

## 📈 7. Proactividad y Aprendizaje Continuo
Un agente de este proyecto no es un mero ejecutor, sino un **arquitecto proactivo**.

1.  **Registro de Lecciones (`LESSONS.md`)**: Tras resolver un bug complejo o implementar una optimización crítica, el agente **DEBE** actualizar el archivo `LESSONS.md`. Esto evita repetir errores costosos.
2.  **Anticipación de Deuda Técnica**: Si detectas una lógica que funcionará ahora pero fallará cuando la colección crezca (ej. falta de virtualización), proponlo inmediatamente.
3.  **Refactorización Oportunista**: Si estás tocando un método y ves código redundante o ineficiente, mejóralo siguiendo la regla del "Boy Scout" (deja el código mejor de como lo encontraste), siempre que no rompa la compatibilidad.
4.  **Evolución del Conocimiento**: Este archivo `agents.md` no es estático. Si descubres un nuevo patrón que funciona mejor para el proyecto, actualízalo.

---
*Este archivo se actualiza tras cada hito importante. Consulta siempre la fecha de la última actualización en MEMORY.md.*
