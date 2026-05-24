# Gestor de Juegos

Aplicación para gestionar colecciones de juegos retro, permitiendo organizar juegos por plataformas, importar desde LaunchBox, descargar carátulas y lanzar emuladores.

## Cambios Recientes

- **Soporte de Metadatos Maestro:** Integración directa con `LaunchBox.Metadata.db` para importar descripciones (Overview), géneros y valoraciones de la comunidad.
- **Sincronización Inteligente de MAME:** Uso de `ShortName` para vincular ROMs y clones con precisión quirúrgica desde los XML de LaunchBox.
- **Interfaz Enriquecida:**
    - Panel de detalles con descripción completa, versión y logotipos dinámicos.
    - Menú contextual (botón derecho) en juegos para acciones rápidas (Jugar, Editar, Favoritos, Eliminar).
    - Botones de acción directa para abrir carpetas de ROMs.
- **Arquitectura de Base de Datos Dual:** Separación de datos de juegos (GestorJuegos.db) y multimedia (GestorCovers.db) para mayor rendimiento.
