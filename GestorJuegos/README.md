# Gestor de Juegos

Aplicación para gestionar colecciones de juegos retro, permitiendo organizar juegos por plataformas, importar desde LaunchBox, descargar carátulas y lanzar emuladores.

## Cambios Recientes

- **Corrección de Error Crítico:** Se ha corregido un bug que provocaba el cierre inesperado de la aplicación al intentar guardar una nueva plataforma.
- **Mejoras de Robustez:** 
    - Implementado manejo de errores (try-catch) en los formularios de gestión de plataformas.
    - Validación de nombres duplicados al añadir plataformas.
    - Mejora en la consulta de estadísticas de plataformas para evitar excepciones por valores nulos o duplicados.
- **Arquitectura de Base de Datos Dual:** Separación de datos de juegos (GestorJuegos.db) y multimedia (GestorCovers.db) para mayor rendimiento.
