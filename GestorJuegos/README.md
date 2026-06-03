# Gestor de Juegos

Aplicación para gestionar colecciones de juegos retro, permitiendo organizar juegos por plataformas, importar desde LaunchBox, descargar carátulas y lanzar emuladores.

## Cambios Recientes (v1.4.1-Dev)

- **Vistas Dinámicas (Views)**: Implementadas nuevas disposiciones de juegos inspiradas en LaunchBox/BigBox: "Wheel Vertical" y "Horizontal Wheel/CoverFlow".
- **Selección por Tema**: El tema ahora puede dictar la vista preferida (`PreferredView`) en su archivo `theme.json`.
- **Transiciones Fluídas**: Añadidas animaciones de opacidad y transformación al cambiar de tema, logo o seleccionar juegos para una experiencia premium.
- **Soporte de Metadatos Maestro:** Integración directa con `LaunchBox.Metadata.db` para importar descripciones (Overview), géneros y valoraciones de la comunidad.
- **Arquitectura de Temas 2.0**: Temas 100% externos con soporte para fuentes, logos y overlays personalizados.
