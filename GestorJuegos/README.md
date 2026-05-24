# Gestor de Juegos

Aplicación para gestionar colecciones de juegos retro, permitiendo organizar juegos por plataformas, importar desde LaunchBox, descargar carátulas y lanzar emuladores.

## Cambios Recientes

- **Soporte de Metadatos Maestro:** Integración directa con `LaunchBox.Metadata.db` para importar descripciones (Overview), géneros y valoraciones de la comunidad.
- **Sincronización Inteligente de MAME:** Uso de `ShortName` para vincular ROMs y clones con precisión quirúrgica desde los XML de LaunchBox.
- **Interfaz Enriquecida e Inmersiva (Estilo LaunchBox Premium v1.1.3):**
    - Rediseño completo del panel derecho de detalles replicando fielmente LaunchBox.
    - Cabecera inmersiva con logotipo del juego superpuesto sobre el banner (el banner se mantiene visible debajo del logo y el botón redundante de galería se oculta automáticamente para un diseño limpio).
    - Previsualización dinámica de capturas (corregido para ocultar el placeholder 'Sin captura de pantalla' cuando se muestra una captura o portada).
    - TabControl moderno con pestañas para "Descripción general" (con carrusel de capturas y botón JUGAR verde gigante) y "Juegos Relacionados".
- **Arquitectura de Base de Datos Dual:** Separación de datos de juegos (GestorJuegos.db) y multimedia (GestorCovers.db) para mayor rendimiento.
