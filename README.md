# 🎮 Gestor de Juegos (v1.1.0.0)

Organizador de colecciones de videojuegos para Windows, optimizado para grandes bibliotecas y uso con mando (Gamepad).

![Captura](GestorJuegos/img/captura.png)

## 🚀 Características Principales

- **Muro de Plataformas**: Navegación visual moderna con logos oficiales de consolas.
- **Dashboard Dinámico**: Estadísticas rápidas, juegos recientes y acceso directo a sistemas.
- **Estadísticas Detalladas**: Desglose por regiones, plataformas y géneros con banderas visuales.
- **Modo Mando**: Navegación completa optimizada para mandos mediante XInput.
- **Optimización Crítica**: Inserciones por lotes y miniaturas pre-generadas para fluidez máxima.

## 📅 Historial de Versiones

### v1.1.0.0 (Actual)
- **Rediseño Estilo LaunchBox**: Implementación de un muro visual de plataformas que carga artes locales.
- **Centro de Estadísticas**: Nueva vista detallada con gráficas de distribución y recuentos de colección.
- **Actualización de Navegación**: Flujo de usuario mejorado centrado en la selección visual de sistemas.

### v1.0.9.9
- **Eliminación de Integración EmuMovies**: Retirada del soporte para la API de EmuMovies para favorecer el uso de artes locales y herramientas externas.
- **Limpieza de Interfaz**: Eliminación de credenciales y campos de búsqueda redundantes.

### v1.0.9.8
- **Optimización Crítica de Rendimiento**: Implementación de inserciones y actualizaciones por lotes (Batch Insert) en todos los importadores.
- **Configuración Global Persistente**: Nuevo panel de configuración para gestionar rutas de LaunchBox, preferencias de arte y credenciales de EmuMovies.
- **Integración Avanzada con LaunchBox**: Importación automática de carátulas locales (Box Front, 3D, etc.) durante el escaneo de plataformas.
- **Selector Dinámico de Arte**: Posibilidad de alternar entre diferentes tipos de imágenes locales desde el panel de detalles.
- **Refuerzo de Arquitectura Dual**: Eliminación de datos multimedia redundantes de la base de datos principal y uso de `[NotMapped]` para mayor integridad.

### v1.0.9.7
- **Persistencia de Configuración**: La ruta de LaunchBox se guarda en `appsettings.json` tras la primera selección.
- **UX de Importación**: Validación inteligente de carpetas de LaunchBox para asegurar instalaciones válidas.

### v1.0.9.6
- **Importador Nativo LaunchBox**: Lectura directa de XML de plataformas con extracción de metadatos (Géneros, Años, Rutas, Favoritos).
- **Limpieza de Scrapers**: Eliminación de IGDB, TGDB, GameTDB y PalSnes. Vimm's Lair queda como única fuente online.
- **Refactorización**: Creación de `IgdbSearchResult.cs` como modelo compartido para desacoplar la UI de los servicios eliminados.

### v1.0.9.5
- **Arquitectura Dual DB**: Metadatos en `GestorJuegos.db` y multimedia en `GestorCovers.db`.
- **Sistema de Miniaturas**: Integración de SkiaSharp para generación automática de caché visual (200x300px).
- **Respaldo Integral**: Panel de exportación selectiva para ambas bases de datos.
- **Drag & Drop Recursivo**: Las carpetas se importan como plataformas automáticas escaneando subdirectorios.

### v1.0.9.4
- **Dashboard Visual**: Estadísticas de colección, barra de progreso de carátulas y top de regiones.
- **Buscador Global**: Acceso instantáneo a cualquier juego de la colección desde el dashboard.
- **Filtros Temporales**: Ordenación por "Recién añadidos" y "Antiguos".

## 🛠️ Requisitos e Instalación

1. Tener instalado .NET 8 SDK.
2. Clonar el repositorio.
3. Ejecutar `dotnet run` dentro de la carpeta del proyecto.

---
Desarrollado con ❤️ por Gemini CLI.
