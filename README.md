# 🎮 Gestor de Juegos

![AvaloniaUI](https://img.shields.io/badge/Avalonia-12.0.2-purple.svg?style=for-the-badge&logo=avalonia)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4.svg?style=for-the-badge&logo=dotnet)
![SQLite](https://img.shields.io/badge/SQLite-003B57.svg?style=for-the-badge&logo=sqlite)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-9.0-blue.svg?style=for-the-badge)

Una aplicación de escritorio moderna construida con **Avalonia UI** y **.NET 9**, diseñada para catalogar y gestionar colecciones de videojuegos retro. Permite organizar los juegos por plataformas, almacenar detalles como el año de lanzamiento, género y mantener un registro visual con la carátula de cada juego.

---

## 🚀 Registro de Cambios (Changelog)

**Version 1.0.0 (14-05-2026)**
- Integración con API de IGDB para descarga automática de metadatos y carátulas.
- Añadidos iconos de aplicación y ventana.
- Añadida lectura automática de la versión para la barra de título de la ventana.
- **Nuevo:** Funcionalidad de Launcher (Ejecutar juegos / emuladores desde la app) (mira Issue #3).
- **Nuevo:** Importar Formato (.XML , .DAT) - Importar catálogos completos en formato XML (No-Intro / DAT) (mira Issue #1).
- **Nuevo:** Auto-descarga masiva de carátulas (Scraping en lote) y actualización de metadatos (mira Issue #2).

---

## ✨ Características Principales

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

## 📸 Capturas de Pantalla

*(Aquí puedes añadir capturas de la interfaz una vez que personalices los estilos y comiences a añadir juegos).*

---


## 🔑 Configuración de la API (IGDB)
Para que el buscador automático de juegos funcione, necesitas configurar tus credenciales de IGDB.
Crea un archivo llamado `appsettings.json` en la misma carpeta que el ejecutable o en la raíz del proyecto (este archivo se ignora en Git por seguridad) con el siguiente formato:
```json
{
  "IGDB": {
    "ClientId": "TU_CLIENT_ID",
    "ClientSecret": "TU_CLIENT_SECRET"
  }
}
```
Obtén tus credenciales registrando una aplicación en la [Consola de Desarrolladores de Twitch](https://dev.twitch.tv/console).

---
**Desarrollado con ❤️ para los amantes del coleccionismo de videojuegos.**
