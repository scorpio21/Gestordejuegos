# Resumen de Cambios: Formato Premium de Launchbox (v1.1.2.4-Dev)

Hemos evolucionado la aplicación para añadir un sistema de organización y navegación jerárquica avanzado en la barra lateral izquierda, emulando fielmente la experiencia visual y funcional de **Launchbox** y dotando al sistema de total resiliencia frente a bases de datos SQLite preexistentes.

---

## 🛠️ Cambios Realizados en v1.1.2.4-Dev (Actual)

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
