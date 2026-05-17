# 🎮 Gestor de Juegos

![AvaloniaUI](https://img.shields.io/badge/Avalonia-12.0.2-purple.svg?style=for-the-badge&logo=avalonia)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4.svg?style=for-the-badge&logo=dotnet)
![SQLite](https://img.shields.io/badge/SQLite-003B57.svg?style=for-the-badge&logo=sqlite)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-9.0-blue.svg?style=for-the-badge)

Una aplicación de escritorio moderna construida con **Avalonia UI** y **.NET 9**, diseñada para catalogar y gestionar colecciones de videojuegos retro. Permite organizar los juegos por plataformas, almacenar detalles como el año de lanzamiento, género y mantener un registro visual con la carátula de cada juego.

---

## 📸 Capturas de Pantalla

<div align="center">
  <img src="GestorJuegos/img/captura.png" alt="Captura de la aplicación Gestor de Juegos" width="800"/>
</div>

---

## 🚀 Registro de Cambios (Changelog)

**Version 1.0.8 (Actual)**

- **Nuevo:** Soporte **Drag & Drop** para archivos comprimidos (`.zip`, `.7z`, `.rar`) y ROMs directas. Arrastra archivos al listado para añadirlos automáticamente con nombre, región e idiomas procesados.
- **Mejora:** Selector de **Sistema Oficial** para Vimm's Lair. Ahora puedes elegir el sistema exacto antes de la descarga masiva para garantizar resultados perfectos.
- **Mejora:** Validación de existencia en Vimm. El sistema comprueba si la plataforma existe antes de iniciar el escaneo masivo.
- **Mejora:** Motor de Importación inteligente. Ahora distingue entre carpetas de marca (ej: "Sega") y plataformas reales, y detecta automáticamente juegos dentro de carpetas `Games` o `Roms`.
- **Nuevo:** Gestión de exclusiones mediante **`blacklist.json`** personalizable y compatible con caracteres especiales (tildes, ñ).

**Version 1.0.7**

**Version 1.0.6**

- **Mejora:** Optimización para colecciones masivas (MAME, etc.). Escaneo ultra-rápido mediante sistema de caché por ruta en memoria.

**Version 1.0.5**

- **Nuevo:** Importación Avanzada por Carpetas. Ahora puedes importar toda tu colección seleccionando una carpeta raíz. El sistema crea las plataformas automáticamente basándose en las subcarpetas y vincula las ROMs.
- **Nuevo:** Escaneo de Carátulas Locales. Permite asociar imágenes a tus juegos desde una carpeta local mediante coincidencia de nombres recursiva.

**Version 1.0.4**

- **Nuevo:** Soporte para Mando (Gamepad Support). Navegación completa por la cuadrícula y lista de juegos mediante mandos tipo Xbox/PlayStation utilizando el D-Pad o el stick analógico izquierdo. Lanzamiento rápido de juegos con el botón `A` y cancelación de menús con `B` (mira Issue #9).

**Version 1.0.3 (16-05-2026)**

- **Nuevo:** Sistema de Filtros Avanzados. Añadido un menú desplegable para filtrar la vista actual por Región, Año de lanzamiento o Favoritos.
- **Nuevo:** Juegos Favoritos. Ahora puedes marcar tus juegos preferidos con una estrella (⭐) para destacarlos visualmente y filtrarlos rápidamente (mira Issue #10).

**Version 1.0.2 (16-05-2026)**

- **Nuevo:** Soporte Multi-Disco. Posibilidad de añadir múltiples archivos de ROM a un mismo juego (ej. Disco 1, Disco 2) y elegir cuál ejecutar desde la interfaz, manteniéndolo organizado bajo una única entrada en tu colección (mira Issue #8).

**Version 1.0.1 (16-05-2026)**

- **Nuevo:** Perfiles Avanzados de Emuladores. Permite sobrescribir la configuración del emulador a nivel de juego para usar diferentes emuladores dentro de una misma plataforma (mira Issue #7).
- **Nuevo:** Menú de Ayuda interactivo para enseñar a configurar emuladores y argumentos de lanzamiento.

**Version 1.0.0 (14-05-2026)**

- Integración con API de IGDB para descarga automática de metadatos y carátulas.
- Añadidos iconos de aplicación y ventana.
- Añadida lectura automática de la versión para la barra de título de la ventana.
- **Nuevo:** Funcionalidad de Launcher (Ejecutar juegos / emuladores desde la app) (mira Issue #3).
- **Nuevo:** Buscador en tiempo real por título y género (mira Issue #4).
- **Nuevo:** Paginación (Lazy Loading) para optimizar colecciones de miles de juegos sin usar memoria (mira Issue #5).
- **Nuevo:** Vista de Dashboard y estadísticas globales como pantalla principal (mira Issue #6).
- **Nuevo:** Importar Formato (.XML , .DAT) - Importar catálogos completos en formato XML (No-Intro / DAT) (mira Issue #1).
- **Nuevo:** Auto-descarga masiva de carátulas (Scraping en lote) y actualización de metadatos (mira Issue #2).
- **Nuevo:** Importar listas desde archivo de texto (.txt) mediante Drag & Drop inteligente, omitiendo duplicados.
- **Nuevo:** Soporte multi-scraper en búsquedas manuales y masivas, permitiendo usar **IGDB**, **TheGamesDB** o **GameTDB** (optimizado para Wii, 3DS, Switch, etc.).
- **Mejora:** La detección de duplicados en la importación ahora diferencia regiones (Ej. `Spain` vs `Europe`) permitiendo mantener múltiples versiones del mismo juego.
- **Mejora:** Filtrado y priorización inteligente de carátulas en IGDB dependiendo de la plataforma seleccionada.

---

## ✨ Características Principales

- **Buscador Dinámico:** Filtra instantáneamente tu colección de juegos por título o género a medida que escribes.
- **Importación por Drag & Drop:** Arrastra y suelta archivos `.txt` sobre la aplicación para poblar plataformas enteras en segundos, reconociendo región e idiomas automáticamente y permitiendo versiones por región (e.g. Español, Europeo, USA).
- **Auto-Scraping Multifuente:** Busca metadatos y carátulas de forma manual o masiva usando **IGDB**, **TheGamesDB** o **GameTDB**, priorizando la consola donde se esté buscando.

- **Gestión por Plataformas Dinámica:** Crea, edita y elimina tus propias plataformas (PlayStation, Super Nintendo, etc.) desde un menú de gestión interactivo.
- **Vistas Personalizables:** Alterna entre una **vista de Lista** detallada o una **vista de Galería (Cuadrícula)** enfocada en las carátulas.
- **CRUD Completo:** Añade, visualiza, edita y elimina información sobre tus juegos.
- **Campos Detallados:** Soporte para nombre, año, género, región y múltiples **idiomas** (Ej. En,Fr,Es).
- **Soporte de Imágenes:** Capacidad de adjuntar y almacenar carátulas o portadas nativamente en la base de datos local.
- **Base de Datos Embebida:** Utiliza SQLite para almacenar todos los datos de forma local, rápida y portátil. No requiere servidores.
- **Diseño Dark Premium:** Interfaz elegante con estética moderna, bordes redondeados y colores cuidados para menor fatiga visual.

## 🛠️ Tecnologías

- **C# / .NET 9.0:** Motor principal de la aplicación.
- **Avalonia UI (v12.0.2):** Framework multiplataforma utilizado para crear una interfaz de usuario fluida, moderna e independiente del sistema operativo.
- **Entity Framework Core 9:** ORM encargado del manejo y comunicación con la base de datos.
- **SQLite:** Motor de base de datos ligero y robusto para almacenamiento local en el cliente.

---

## 📁 Estructura del Proyecto

El proyecto está organizado de manera modular, facilitando su escalabilidad y fácil mantenimiento en el futuro:

```text
GestorJuegos/
├── Data/            # Configuración de Entity Framework y Contexto SQLite (AppDbContext)
├── Models/          # Modelos de datos y entidades de negocio (Game, Platform)
├── Services/        # Lógica de negocio y abstracción de la base de datos (GameService)
├── Utils/           # Utilidades y Convertidores (ByteArrayToBitmapConverter)
├── GestorJuegos/    # Interfaz gráfica y vistas de Avalonia (MainWindow.axaml)
└── GestorJuegos.sln # Archivo de solución principal de Visual Studio
```

---

## 🚀 Instalación y Uso

### Prerrequisitos

- [SDK de .NET 9.0](https://dotnet.microsoft.com/download) instalado en tu equipo.
- **Visual Studio 2022** (Recomendado).
- Extensión **[Avalonia para Visual Studio 2022](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS)** (para previsualizar la interfaz de usuario en el diseñador).

### Pasos

1. **Clonar o descargar** el repositorio.
2. Navegar a la carpeta raíz del proyecto y abrir la solución `GestorJuegos.sln` con Visual Studio 2022.
3. Compilar el proyecto pulsando `F6` (o desde el menú Compilar -> Compilar solución). En este paso se restaurarán automáticamente los paquetes de NuGet necesarios.
4. Ejecutar el programa pulsando `F5`.

> **Nota:** En su primer arranque, la aplicación generará de forma automática el archivo `GestorJuegos.db` en el directorio de ejecución, listo para que comiences a añadir tus plataformas y juegos.

---

## 🔑 Configuración de las APIs (IGDB / TheGamesDB)

Para que el buscador automático de juegos funcione, necesitas configurar tus credenciales de las APIs.
Crea (o deja que el programa cree por primera vez) un archivo llamado `appsettings.json` en la misma carpeta que el ejecutable (este archivo se ignora en Git por seguridad) con el siguiente formato:

```json
{
  "IGDB": {
    "ClientId": "TU_CLIENT_ID",
    "ClientSecret": "TU_CLIENT_SECRET"
  },
  "TheGamesDB": {
    "ApiKey": "TU_API_KEY_DE_THEGAMESDB"
  }
}
```

- **IGDB:** Obtén tus credenciales registrando una aplicación en la [Consola de Desarrolladores de Twitch](https://dev.twitch.tv/console).
- **TheGamesDB:** Obtén tu clave en la web oficial o usa una proporcionada. El campo puede dejarse en blanco si no se usa esta fuente. GameTDB funciona de manera pública.

---

## 📄 Licencia

Este proyecto está bajo la Licencia Pública General de GNU v3.0 (GPL-3.0). Para más detalles, consulta el archivo [LICENSE](LICENSE) incluido en este repositorio.

---
**Desarrollado con ❤️ para los amantes del coleccionismo de videojuegos.**
