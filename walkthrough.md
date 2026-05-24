# Resumen de Cambios: Formato Premium de Launchbox (v1.1.2.5-Dev)

Hemos evolucionado la aplicación para añadir una réplica exacta del panel derecho de detalles de LaunchBox para plataformas y categorías de sistemas, logrando independencia total para el usuario final mediante el almacenamiento local de metadatos técnicos y fotos físicas en SQLite.

---

## 🛠️ Cambios Realizados en v1.1.2.5-Dev (Actual)

### 1. Panel Derecho de Detalles de Sistemas (`MainWindow.axaml` y `MainWindow.axaml.cs`)
* **Visualización Dinámica e Integrada (`PnlPlatformDetails`)**:
  * Compartiendo espacio en la columna derecha, se activa automáticamente cuando se hace clic en cualquier nodo de plataforma o categoría en el árbol lateral.
  * Muestra el logotipo Clear Logo de la plataforma/categoría cargado localmente de la DB.
  * Incluye la foto física de hardware de la consola o microordenador (`ImgPlatformHardware`) leída desde bytes guardados en la columna `HardwareImage`.
  * Diseñada una tabla/grid estilizada con fondo oscuro para las especificaciones técnicas completas.
  * Incluye una sección de descripción histórica con scroll vertical dedicada a las notas del sistema.
* **Cálculo Dinámico de Estadísticas Locales**:
  * Suma y visualiza en tiempo real: Juegos en total, juegos completados, última vez jugado, veces jugado en total y tiempo total de juego formateado en `Xh YYm ZZs`.
  * Muestra dinámicamente el nombre del "Último Juego Jugado" y del "Juego Más Jugado" analizando los registros de ejecución locales.
  * Soporte para estadísticas agregadas de la colección completa y por categorías completas (ej: Computadores, Consolas y Portátiles).

### 2. Importador Asíncrono Portativo (`MainWindow.axaml.cs`)
* **Scraper de Metadatos y Fotos de Consolas (`ImportLaunchBoxAssets`)**:
  * Conexión en segundo plano a la base de datos maestra externa de LaunchBox (`LaunchBox.Metadata.db`) para copiar especificaciones de hardware (CPU, RAM, Gráficos, Sonido, Soporte, Desarrollador, Fabricante, etc.) directamente a la base local de la aplicación.
  * Escaneo del directorio de LaunchBox para hallar las fotos del hardware (`Images/Platforms/[Name]/Console/`) y guardarlas binariamente.
  * Inserción de descripciones detalladas por defecto para las categorías conceptuales ("Computers", "Consoles", "Handhelds").

### 3. Migraciones y Modelado de Datos (`Platform.cs`, `PlatformCategory.cs` y `GameService.cs`)
* **Nuevas Columnas de Hardware**: Agregadas propiedades correspondientes a CPU, Memory, Graphics, Sound, Display, Media, Notes, ReleaseDate, Developer, Manufacturer y la imagen binaria `HardwareImage` en `Platform.cs`.
* **Auto-parcheo Silencioso**: `GameService.cs` ejecuta automáticamente comandos SQL `ALTER TABLE` y `CREATE TABLE IF NOT EXISTS` en el arranque para parchar bases de datos previas de forma segura.

### 4. Rediseño Premium de la Rejilla de Juegos (`MainWindow.axaml` y `Game.cs`)
* **Aspecto Limpio y Flotante (LaunchBox Grid)**:
  * Removido el marco rectangular oscuro y rígido de la rejilla. Las carátulas ahora flotan directamente sobre la interfaz de fondo.
  * Cambiado el modo de visualización de la imagen a `Stretch="Uniform"`. Ahora las cajas (Sega 32X, Arcade, NES, etc.) preservan perfectamente sus proporciones originales sin estirarse ni recortarse.
  * Implementada sombra dinámica *shrink-wrap* (`BoxShadow="0 5 12 0 #90000000"`) en el borde contenedor que se amolda exactamente a las dimensiones reales y formas proporcionales del box art, en lugar de un área fija.
* **Metadatos Integrados Debajo**:
  * El Título y el Desarrollador se reubicaron en un panel limpio debajo de la carátula, alineados a la izquierda.
  * Creada la propiedad inteligente `GridSubtext` en `Game.cs` para resolver en segundo plano el metadato del subtexto con una estrategia de fallback de tres capas: `Developer` -> `Publisher` -> `Year`.
* **Placeholder de Gamepad Dinámico**:
  * Diseñado un cuadro de placeholder redondeado sutil (`#2e384d`) con un icono de control (`🎮`) que se activa únicamente si la carátula es nula mediante conversores estáticos de Avalonia, evitando solapamientos laterales de fondo en cajas personalizadas.
* **Resplandor Azul Neón en Selección**:
  * Añadido estilo visual de selección (`ListBoxItem:selected`) que aplica un contorno y sombra de brillo azul eléctrico (`#00a2ff`) en el borde exacto de la carátula o placeholder seleccionado.

---

## 🛠️ Cambios Realizados en v1.1.2.4-Dev (Anterior)

### 1. Barra Lateral Jerárquica y Navegación Dinámica (`MainWindow.axaml`)
* **Barra Lateral con `TreeView` (`TvSidebar`)**:
  * Reemplazado el antiguo ListBox por un árbol de navegación jerárquico.
  * Agrupación automática de plataformas bajo sus categorías principales: **Consoles** (🕹️), **Computers** (🖥️) y **Handhelds** (📟).
  * Soporte para mostrar iconos pixel-art binarios de cada plataforma/categoría extraídos de la base de datos, con emojis de respaldo si no hay binario disponible.
  * Expansión de nodos habilitada por defecto y contadores de juegos dinámicos visibles por cada categoría y plataforma.
* **Selector de Vistas Superior (`CmbSidebarView`)**:
  * Implementado un menú desplegable superior sobre la barra lateral que permite alternar la vista entre:
    * *Categoría de Plataforma* (Vista jerárquica por defecto).
    * *Plataformas* (Lista plana tradicional).
    * *Géneros* (Filtro por etiquetas de género).
    * *Regiones* (Filtro geográfico con banderas).
    * *Biblioteca* (Acceso rápido a Favoritos).

### 2. Clasificación Inteligente y Scraper de Logotipos (`MainWindow.axaml.cs`)
* **Lógica del Árbol (`LoadPlatforms`)**:
  * Mapeo de datos dinámico que genera los nodos jerárquicos según la vista del combobox y calcula de forma agregada el número total de juegos por categoría sumando sus nodos hijo.
  * Filtrado recursivo al hacer clic en nodos principales de categoría (por ejemplo, al hacer clic en "Computers" se visualizan todos los juegos de todos los microordenadores).
* **Importación y Caché Multimedia (`ImportLaunchBoxAssets`)**:
  * Hilo en segundo plano asíncrono para escanear y guardar binariamente en SQLite los iconos pixel-art y logotipos Clear Logo desde la ruta local de LaunchBox.
* **Reclasificación de Categoría Inteligente**:
  * Algoritmo de detección que analiza el nombre de la plataforma y la mueve de la categoría genérica "Consoles" a "Computers" (ej: Amiga, Spectrum, MSX, PC) o "Handhelds" (ej: Game Boy, PSP, Game Gear, DS) guardando este cambio de forma persistente.

### 3. Modelo de Datos y Esquema en Caliente (`GameService.cs` y `AppDbContext.cs`)
* **Modelo de Categorías (`PlatformCategory.cs` y `Platform.cs`)**:
  * Creada la entidad `PlatformCategory` con campos para nombre, icono y banner Clear Logo.
  * Añadidas las propiedades de base de datos `Logo` e `Icon` en `Platform.cs` para almacenar el arte de plataformas de forma local.
* **Auto-parcheo de Esquema SQLite**:
  * Agregadas sentencias `ALTER TABLE` y `CREATE TABLE IF NOT EXISTS` controladas con bloques `try-catch` en el constructor de `GameService.cs`.
  * Esto permite que, si ejecutas el gestor con una base de datos ya existente en tu PC, se actualice su estructura agregando las nuevas columnas e indexaciones sin lanzar fallos críticos ni requerir migraciones complejas de Entity Framework.

---

## 🛠️ Cambios Realizados en v1.1.2.3-Dev

### 1. Interfaz de Usuario de Detalles (`MainWindow.axaml`)
* **Cabecera Inmersiva (Banner)**:
  * Fondo multimedia del juego con gradiente de desvanecimiento premium al azul oscuro de la aplicación.
  * Logotipo transparente superpuesto dinámicamente (`BrdDetailLogo`).
  * Indicador de calificación dinámica por estrellas en tono azul cielo (`TxtInfoRatingText` y `TxtInfoRatingStars`).
  * Botones flotantes inmersivos de acción rápida en la esquina superior izquierda (pantalla completa) y centro (Ver todas las imágenes).
  * Panel flotante compacto en la parte inferior derecha con accesos rápidos a **Editar** (✏️), **Favorito** (❤️), **Abrir Carpeta** (📁) y **Eliminar** (🗑️) mapeados a los eventos de C# originales.
* **Sección de Título**:
  * Indicador estilizado de plataforma en mayúsculas (ej: `🕹️ ARCADE`).
  * Título del juego en tipografía extra negrita de 26px (`TxtInfoName`).
* **Pestañas de Detalle Modernas (`TabControl`)**:
  * Alternador premium entre las pestañas "Descripción general" y "Juegos Relacionados".
* **Pestaña "Descripción general"**:
  * Visualizador de captura de pantalla grande (`ImgGameplayPreview`) interactivo.
  * Carrusel de miniaturas horizontal (`LstScreenshots`) para alternar capturas y artes del juego.
  * Botón **JUGAR** verde gigante estilo Launchbox en tres secciones (icono de Play, texto y selector desplegable de disco).
  * Panel de detalles básicos en formato de tabla limpia (Fecha de lanzamiento, Desarrollador, Distribuidor y Tiempo Jugado).
  * Selector del tipo de carátula (`CmbArtType`) y visualizador asociado (`ImgCover`) integrado elegantemente.
  * Caja redondeada de "INFORMACIÓN": Clasificación, Género, Series, Modo de Juego, Progreso (con colores de estado dinámicos), Estado de emulación, Fuente, Portable (No), Archivo de la ROM y Última vez jugado.
  * Iconos rápidos de internet (Wikipedia 🌐 y YouTube ▶) en la cabecera del panel.
  * Caja redondeada de "DESCRIPCIÓN" con espaciado de línea y fuentes curadas.
* **Pestaña "Juegos Relacionados"**:
  * Listado vertical interactivo de juegos de la misma plataforma (`LstRelatedGames`) con miniaturas de portada y género.
* **Visor de Pantalla Completa (`OverlayImageViewer`)**:
  * Creado un overlay superpuesto inmersivo en color negro para disfrutar en alta resolución de las capturas del juego o la portada a gran escala.

### 2. Lógica del Backend (`MainWindow.axaml.cs`)
* **Calificación y Estrellas**: Mapeo y cálculo matemático dinámico para convertir la escala de calificación de la base de datos (0-100) a estrellas (0-5) con tipografía `★` y `☆`.
* **Corazón de Favorito**: Sincronización del icono (corazón lleno `♥` o vacío `♡` y colores rojo o blanco) al seleccionar juegos y al hacer clic, actualizando el estado y la base de datos en tiempo real.
* **Visor Completo y Galería**:
  * Consulta inteligente a `CoversDbContext` de capturas (`Screenshot - Gameplay`, `Snap`, `Fanart - Background`) para llenar el carrusel de capturas adicionales.
  * Implementación del visor de pantalla completa interactivo con gestos de cierre al pulsar fuera de la imagen.
* **Menú Desplegable JUGAR Multi-Disco**:
  * Implementado un algoritmo dinámico que detecta múltiples ROMs/discos vinculados en el juego. Si hay más de uno, rellena el desplegable del botón verde JUGAR con opciones como `Disco 1 (nombre.zip)`, `Disco 2...` permitiendo lanzar de forma individual cualquiera de los discos integrados.
* **Enlaces Wikipedia & YouTube**:
  * Automatización de consultas en el navegador para buscar artículos y trailers basados en el título del juego.
* **Juegos Relacionados**:
  * Carga automática de títulos semejantes de la misma plataforma. Al pulsar sobre cualquier juego sugerido, la aplicación realiza una navegación instantánea cargando toda la información del nuevo juego seleccionado.

### 3. Ajustes y Correcciones de Interfaz (v1.1.2.3-Dev)
* **Solución al Solapamiento del Logotipo**:
  * Se corrigió la superposición del botón "Ver todas las imágenes" detrás de los logotipos dinámicos transparentes. Ahora, el botón se oculta automáticamente si el juego dispone de un logotipo (`BtnViewAllImages.IsVisible = !hasLogo`), dejando el logotipo limpio e imponente en el centro.
  * Se aseguró la visibilidad del banner/fanart de fondo (`ImgDetailBackground.IsVisible = true`) debajo del logotipo transparente en lugar de ocultarlo, logrando la verdadera y premium experiencia estética inmersiva de LaunchBox.
* **Control de Placeholder de Captura**:
  * Se asignó un identificador al control del placeholder del visor de gameplay (`Name="TxtGameplayPlaceholder"`).
  * Se corrigió la regla de visualización redundante que mostraba "Sin captura de pantalla" por encima de las imágenes de previsualización (cuando se utilizaba la portada como fallback). Ahora, la visibilidad se gestiona de forma programática y robusta desde C# (`TxtGameplayPlaceholder.IsVisible = false` cuando la imagen está cargada correctamente).

---

## 🧪 Pruebas de Validación Ejecutadas
1. **Compilación del Proyecto**:
   * Ejecutado `dotnet build` obteniendo **compilación correcta con cero errores**.
2. **Verificación de Eventos**:
   * Verificado el correcto funcionamiento del enlazado de eventos del constructor de C# con los nombres de control unificados (`BtnEditGame`, `BtnOpenFolder`, `BtnDelete`, `ImgCover`).
3. **Validación del Esquema en Caliente**:
   * Verificada la persistencia automática al iniciar la aplicación con una base de datos local preexistente, confirmando la creación exitosa de las columnas de iconos e imágenes adicionales sin fallos ni crashes en la interfaz.
