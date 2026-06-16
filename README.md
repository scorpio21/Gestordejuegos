# 🎮 Gestor de Juegos (v2.0.0-Dev)

Organizador de colecciones de videojuegos local para Windows, diseñado para la catalogación masiva, gestión offline de metadatos, soporte de RetroAchievements y navegación inmersiva compatible con mandos (Gamepad).

---

## 📌 Índice de Contenidos

Haga clic en cualquiera de las secciones siguientes para navegar por el documento:

1. [🎮 Introducción](#introduccion)
2. [📂 Estructura del Proyecto](#estructura-del-proyecto)
3. [⚙️ Arquitectura de Servicios y Modelos](#arquitectura-de-servicios-y-modelos)
4. [💾 Sistema de Almacenamiento Offline](#sistema-de-almacenamiento-offline)
5. [🖥️ Guía de Uso de la Interfaz](#guia-de-uso-de-la-interfaz)
6. [🛠️ Requisitos e Instalación](#requisitos-e-instalacion)

---

<a id="introduccion"></a>
## 🎮 Introducción

El **Gestor de Juegos** es una aplicación de escritorio desarrollada bajo la plataforma Avalonia UI y .NET 9. Está diseñada especialmente para entusiastas de la emulación y el coleccionismo que manejan grandes bibliotecas de videojuegos. 

Su filosofía principal es la **autonomía e independencia de red**: una vez importada la colección y los assets multimedia, la aplicación funciona de forma 100% offline, cargando metadatos y arte desde bases de datos locales a altas velocidades gracias a mecanismos de optimización y sistemas de caché en disco.

---

<a id="estructura-del-proyecto"></a>
## 📂 Estructura del Proyecto

El código fuente de la aplicación se encuentra en el directorio [GestorJuegos](file:///k:/GestorJuegos/GestorJuegos) y está organizado de forma modular siguiendo los estándares de arquitectura de componentes:

*   **[Views](file:///k:/GestorJuegos/GestorJuegos/Views/)**: Contiene la definición de la interfaz gráfica en XAML y sus controladores asociados en C#. Está subdividida en:
    *   **[Windows](file:///k:/GestorJuegos/GestorJuegos/Views/Windows/)**: Ventanas principales de la aplicación (como la ventana principal [MainWindow](file:///k:/GestorJuegos/GestorJuegos/Views/Windows/MainWindow.axaml), la ventana modal de ajustes [OpcionesWindow](file:///k:/GestorJuegos/GestorJuegos/Views/Windows/OpcionesWindow.axaml) y el diálogo de selección de colores [ColorPickerDialog](file:///k:/GestorJuegos/GestorJuegos/Views/Windows/ColorPickerDialog.axaml)).
    *   **[Panels](file:///k:/GestorJuegos/GestorJuegos/Views/Panels/)**: Componentes fijos del espacio trilateral de trabajo (barra lateral, barra superior, galería central de juegos, panel de detalles del juego y el fondo dinámico interactivo).
    *   **[Overlays](file:///k:/GestorJuegos/GestorJuegos/Views/Overlays/)**: Diálogos emergentes integrados directamente dentro de la interfaz principal para flujos concretos (edición de metadatos, alertas, confirmaciones de borrado, visores multimedia y diálogos de carga).
    *   **[Items](file:///k:/GestorJuegos/GestorJuegos/Views/Items/)**: Plantillas de representación de elementos repetidos para las listas y grids de la galería.
*   **[Services](file:///k:/GestorJuegos/GestorJuegos/Services/)**: Capa lógica encargada de procesos en segundo plano, interacción con bases de datos, APIs externas e inicio de ejecutables.
*   **[Models](file:///k:/GestorJuegos/GestorJuegos/Models/)**: Clases y entidades del dominio (juegos, plataformas, logros y configuraciones globales).
*   **[Themes](file:///k:/GestorJuegos/GestorJuegos/Themes/)**: Archivos de recursos JSON que definen los esquemas de color dinámicos del programa.
*   **[Sounds](file:///k:/GestorJuegos/GestorJuegos/Sounds/)**: Contiene efectos de sonido WAV de alta calidad utilizados para la navegación inmersiva en la UI.
*   **[Styles](file:///k:/GestorJuegos/GestorJuegos/Styles/)**: Recursos compartidos de estilos y tipografías globales.
*   **[Controls](file:///k:/GestorJuegos/GestorJuegos/Controls/)**: Controles interactivos personalizados.
*   **[Utils](file:///k:/GestorJuegos/GestorJuegos/Utils/)**: Asistentes y utilidades secundarias como soporte de reproducción de audio y formateadores de datos.

---

<a id="arquitectura-de-servicios-y-modelos"></a>
## ⚙️ Arquitectura de Servicios y Modelos

La lógica de negocio está completamente desacoplada de la interfaz gráfica a través de un conjunto de servicios de inyección y comunicación mediante eventos:

### Servicios Clave

*   **GameService ([GameService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/GameService.cs))**: Administra la persistencia de datos de los videojuegos. Gestiona las consultas, actualizaciones, borrados y la vinculación y guardado de las portadas e imágenes dentro de la base de datos local SQLite.
*   **ScannerService ([ScannerService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/ScannerService.cs))**: Escanea directorios locales en busca de ROMs y archivos ejecutables de videojuegos. Emplea alias de consolas para su vinculación y algoritmos de emparejamiento flexible bidireccional para asociar carátulas locales, incluso con variaciones regionales o diferencias en el nombre de los archivos.
*   **LauncherService ([LauncherService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/LauncherService.cs))**: Lanza de forma segura las aplicaciones y emuladores asociados con los juegos de la colección. Configura dinámicamente los argumentos de ejecución y mantiene el registro del tiempo de juego acumulado del usuario en tiempo real.
*   **ImageCacheService ([ImageCacheService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/ImageCacheService.cs))**: Sistema de caché en disco que almacena de forma optimizada en JPG las portadas y capturas extraídas de la base de datos, agilizando drásticamente la carga de la biblioteca visual al evitar continuas lecturas en la base de datos SQL.
*   **RetroAchievementsService ([RetroAchievementsService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/RetroAchievementsService.cs))**: Se conecta con la API de RetroAchievements para la obtención del progreso del usuario, listados de logros bloqueados y desbloqueados, puntuaciones, medallas y la sincronización con la información extendida en pantalla.
*   **ExternalMetadataService ([ExternalMetadataService.cs](file:///k:/GestorJuegos/GestorJuegos/Services/ExternalMetadataService.cs))**: Lector y consultor local para la extracción automatizada de metadatos de juegos desde archivos de bases de datos externas soportadas.

### Modelos de Datos

*   **Game ([Game.cs](file:///k:/GestorJuegos/GestorJuegos/Models/Game.cs))**: Representa las propiedades detalladas de un videojuego en la colección (título, descripción, desarrollador, editor, región, fecha de lanzamiento, favorito, estado de juego, etc.).
*   **Platform y PlatformCategory ([Platform.cs](file:///k:/GestorJuegos/GestorJuegos/Models/Platform.cs), [PlatformCategory.cs](file:///k:/GestorJuegos/GestorJuegos/Models/PlatformCategory.cs))**: Define los datos de una plataforma de hardware emulada (incluyendo CPU, RAM, gráficos, audio, fabricante y la foto física de la consola) y su clasificación dentro de categorías mayores (Ordenadores, Consolas o Portátiles).
*   **AppSettings ([AppSettings.cs](file:///k:/GestorJuegos/GestorJuegos/Models/AppSettings.cs))**: Modelo de guardado persistente para la configuración general del sistema, incluyendo efectos de sonido, rutas externas, temas activos y credenciales de RetroAchievements.

---

<a id="sistema-de-almacenamiento-offline"></a>
## 💾 Sistema de Almacenamiento Offline

La aplicación opera con un sistema de almacenamiento dual offline de SQLite en la raíz del proyecto para garantizar la portabilidad total y cero dependencias de internet durante el juego ordinario:

1.  **Metadatos (`GestorJuegos.db`)**: Guarda los perfiles de los juegos, configuraciones de emulación, especificaciones de consolas y listas de reproducción del usuario. Cuenta con scripts automáticos de migración al arrancar el programa para mantener el esquema de tablas actualizado.
2.  **Multimedia y Arte (`GestorCovers.db`)**: Diseñada específicamente para almacenar de manera centralizada y binaria todo el arte multimedia recopilado (carátulas 2D, portadas 3D, logotipos y capturas de pantalla). Esto permite mover la aplicación entre dispositivos sin perder ninguna imagen y sin llenar el disco de miles de archivos pequeños dispersos.

---

<a id="guia-de-uso-de-la-interfaz"></a>
## 🖥️ Guía de Uso de la Interfaz

La interfaz está dividida en un esquema de tres paneles diseñado para facilitar la interacción y el acceso rápido a los botones:

### 1. Barra Superior (Panel de Control Global)
Situada en la parte alta de la pantalla, agrupa las acciones del sistema:
*   **Añadir Juego**: Botón en el lateral izquierdo que despliega el formulario para añadir nuevos juegos a mano.
*   **Estadísticas**: Abre un panel gráfico emergente con métricas de completado, recuentos de juegos por sistema, tiempos de juego totales y juegos más jugados.
*   **Buscar**: Barra de texto central. Filtrará de inmediato la galería de juegos al escribir cualquier coincidencia de título.
*   **Botón de Tema (Icono Sol/Luna)**: Ubicado en la parte derecha. Permite cambiar instantáneamente el tema de la aplicación entre modo Oscuro (Dark) y modo Claro (Light).
*   **Menús Desplegables**: Permiten cambiar el tipo de arte de carátula visible de forma global (2D, 3D, etc.), ordenar la galería bajo 15 criterios diferentes, realizar sincronizaciones de ROMs o acceder al panel de ayuda.

### 2. Barra Lateral de Navegación (`SidebarView`)
Situada en la izquierda, permite filtrar la galería principal:
*   Muestra un árbol jerárquico que organiza los sistemas de videojuegos en carpetas automáticas (Consolas, Ordenadores, Portátiles).
*   Al seleccionar una plataforma o categoría, la galería filtra los juegos correspondientes, y el panel de detalles derecho muestra estadísticas avanzadas específicas de ese sistema, junto con una reseña histórica detallada y foto del hardware de la consola.

### 3. Galería de Juegos (`LibraryView`)
Ubicada en la zona central:
*   Presenta los juegos disponibles utilizando portadas flotantes con efectos hover adaptativos y resplandor al seleccionarlas. Soporta vistas de rejilla tradicional, lista horizontal, y modos de rueda interactiva (rueda vertical en parábola y rueda horizontal coverflow).
*   **Menú Contextual (Clic Derecho)**: Al hacer clic derecho sobre cualquier carátula de juego en la galería, se despliega un menú contextual rápido que da acceso directo a las herramientas **Editar** y **Eliminar**.

### 4. Panel de Detalles del Juego (`GameDetailsView`)
Ubicado a la derecha, muestra toda la información del juego seleccionado:
*   **Botón de JUGAR**: Botón verde de gran tamaño situado en la parte superior. Ejecuta el emulador correspondiente. En juegos multi-disco, al hacer clic se despliega automáticamente un listado con las diferentes ROMs para seleccionar cuál lanzar.
*   **Favorito Rápido (Icono de Corazón)**: Botón interactivo al lado del botón de Jugar. Permite marcar o desmarcar el juego como favorito en caliente.
*   **Menú de Estado (Icono `•••`)**: Menú emergente de acciones rápidas para cambiar el estado de progreso del juego (Completado, Jugando, Sin Empezar) o borrarlo de la biblioteca.
*   **Visor de Carátula 3D**: Renderizado interactivo en tres dimensiones que permite rotar la caja del juego deslizando el ratón sobre ella.
*   **Visualizador de Logros y Capturas**: En la parte inferior se detalla el listado de logros asociados (RetroAchievements) y un carrusel de capturas de pantalla. Al hacer clic en cualquier captura, esta se abre a pantalla completa en un visor inmersivo.

---

<a id="requisitos-e-instalacion"></a>
## 🛠️ Requisitos e Instalación

Para compilar y ejecutar el Gestor de Juegos de forma local:

1.  Asegúrese de tener instalado el SDK de **.NET 9** en su sistema operativo Windows.
2.  Clone o descargue el código del repositorio en su máquina local.
3.  Abra una terminal o consola de comandos en la carpeta raíz del proyecto.
4.  Ejecute la aplicación mediante el comando:
    ```bash
    dotnet run --project GestorJuegos
    ```
5.  La aplicación se compilará y se iniciará de inmediato en su escritorio.
