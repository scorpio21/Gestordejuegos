# 🎮 Gestor de Juegos (v1.2.0-Dev)

Organizador de colecciones de videojuegos para Windows, optimizado para grandes bibliotecas y uso con mando (Gamepad).

![Captura](GestorJuegos/img/captura.png)

## 🚀 Características Principales

- **Centralización Total Multimedia**: Todas las imágenes (carátulas, logotipos, fanarts y capturas) se almacenan en la base de datos local (`GestorCovers.db`), permitiendo una autonomía completa sin depender de Biblioteca Externa.
- **Muro de Plataformas Profesional**: Navegación visual moderna con logos oficiales de consolas y diseño de cuadrícula.
- **Grupos de Imagen Biblioteca Externa**: Soporte total para categorías (3D Boxes, Cart Art, Clear Logos, etc.) integradas en la DB.
- **Dashboard Dinámico**: Estadísticas rápidas, juegos recientes y acceso directo a sistemas.
- **Modo Mando**: Navegación completa optimizada para mandos mediante XInput.

## 📅 Historial de Versiones

### v1.4.2-Dev (5 Junio 2026)
*   **Corrección Crítica de Estabilidad**: Solucionado el error `System.ObjectDisposedException` que ocurría al cambiar rápidamente de plataforma en la barra lateral.
    - Se ha refactorizado la lógica de `TvSidebar_SelectionChanged` para asegurar que las referencias a la plataforma seleccionada se obtengan antes de cerrar el contexto de la base de datos.
*   **Optimización de Respuesta de Interfaz (Cero Bloqueos)**:
    - **Carga Asíncrona de Plataformas**: `LoadPlatforms` ahora se ejecuta completamente en un hilo secundario (`Task.Run`), evitando que la interfaz se congele al escanear grandes bibliotecas o cargar iconos de disco.
    - **Fondos Dinámicos en Segundo Plano**: La actualización de fanarts y fondos de pantalla (`UpdateDynamicBackground`) se ha convertido a asíncrona, eliminando los micro-tirones (stuttering) al navegar velozmente por la lista de juegos.
    - **Seguridad de Hilos**: Todas las actualizaciones de la UI se gestionan mediante `Dispatcher.UIThread.Post`, garantizando la estabilidad de la aplicación bajo carga.

### v1.3.0-Dev (3 Junio 2026)
*   **Creador de Temas Standalone (`CreadorTemas`)**: Desarrollo e implementación de una aplicación independiente de Avalonia UI para diseñar, editar e importar temas personalizados para `GestorJuegos`.
    - **Independencia absoluta**: Creado fuera del directorio de la aplicación principal para no alterar su código de forma intrusiva.
    - **Detección inteligente**: Autodetecta el directorio `Themes/` del Gestor de Juegos y vincula de forma segura los recursos del tema.
    - **Editor visual interactivo**: Permite personalizar 6 pinceles de color mediante un selector gráfico, definir fuentes de texto locales, configurar imágenes de fondo/superposición, CornerRadius y seleccionar la vista preferida (Cuadrícula, Lista, Rueda Vertical o Rueda Horizontal) con traducción amigable en español.
    - **Personalización de Logotipo y Hover**: Añadida la capacidad de seleccionar un logotipo personalizado (`Images/Logo.png`), elegir un color de borde hover independiente (`HoverBorderBrush`) y graduar el desenfoque de brillo hover (`HoverGlowBlur`).
    - **Vista previa en caliente**: Renderizado en tiempo real de una tarjeta y cabecera de juego simulada con "Aesthetic Wow" reactivo al diseño modificado, incluyendo la simulación dinámica del logo y del efecto hover al pasar el ratón.
*   **Mejoras de Navegación y Vistas (`GestorJuegos`)**:
    - **Persistencia de Vista**: Se ha modificado el cambio de plataforma en la barra lateral para que conserve la vista activa seleccionada por el usuario (Lista, Cuadrícula o Ruedas) en lugar de forzar siempre el restablecimiento a la vista de Cuadrícula.
    - **Hover en Vistas de Rueda**: Se han implementado efectos de hover dinámicos y adaptados para las vistas de Rueda Vertical (iluminación de texto y opacidad) y Rueda Horizontal Coverflow (borde de hover de color y opacidad).
    - **Curvatura y Efecto de Rueda (Vertical y Horizontal)**: Implementado el efecto dinámico de curvatura matemática en parábola para simular una rueda física en el listado. Para la rueda vertical se desplazan los elementos en el eje X, y para la rueda horizontal (Coverflow) en el eje Y. Se calcula y actualiza la curvatura al vuelo en tiempo real en base a la distancia de cada elemento al centro visual.
    - **Estabilización de Curvatura al Cambiar de Plataforma**: Se agregaron manejadores de eventos para `LayoutUpdated` y `EffectiveViewportChanged`, asegurando que el cálculo del efecto de rueda se active inmediatamente cuando los contenedores de elementos terminan de generarse tras un cambio de plataforma.


### v1.2.0.2-Dev (31 Mayo 2026)
*   **Sistema de Temas Dinámico y Extensible (Sin Biblioteca Externa)**: Migrado por completo el motor estático de temas a una arquitectura de carpetas genéricas dinámica e independiente bajo la carpeta `Themes`.
    - Eliminada la carpeta antigua `LBThemes` y todo residuo de Biblioteca Externa (proyectos WPF, XAMLs, etc.).
    - Escaneo dinámico al vuelo en la inicialización de `OpcionesWindow` que pobla el ComboBox de temas leyendo las subcarpetas del directorio `Themes`.
    - Soporte para archivos de configuración `theme.json` por tema, definiendo los colores de recursos dinámicos (`AccentBrush`, `DeepDarkBrush`, `PanelBrush`, `BorderBrush`) e imágenes de fondo personalizadas.
    - Actualización y mapeo en caliente automático desde configuraciones anteriores para compatibilidad retroactiva.
    - Corrección del error crítico de compilación en `MainWindow.axaml.cs` (`CS8641: else duplicado`).

### v1.2.0.1-Dev (31 Mayo 2026)
*   **Nueva Ventana de Opciones estilo Biblioteca Externa**: Diseñada e implementada la ventana de diálogo modal `OpcionesWindow` que replica exactamente la interfaz estética de Biblioteca Externa (panel izquierdo con TreeView jerárquico de categorías y panel derecho de ajustes dinámicos). Soporta y vincula las siguientes configuraciones de forma persistente a `AppSettings` y al archivo `appsettings.json`:
    - Ruta de instalación de Biblioteca Externa (`AppSettings.Biblioteca ExternaPath`) mediante un TextBox y buscador de directorios asíncrono.
    - Efectos de sonido integrados (`AppSettings.EnableSoundEffects`), aplicando los cambios de audio inmediatamente al guardar.
    - Tipo de carátula o arte multimedia preferido por defecto (`AppSettings.PreferredArtType`) e importación automática de carátulas (`AppSettings.AutoImportCovers`).
    - **Personalización Completa de Temas con Cambio en Caliente**: Añadida la categoría completa `Tema de la ventana principal` en el TreeView, con subpaneles funcionales:
      - **Tematización**: Selector dropdown que soporta temas como `"Neon Deluxe Arcade LB"` y `"Old Default"`. Redefine en caliente los pinceles de recursos de Avalonia (`AccentBrush`, `DeepDarkBrush`, `PanelBrush`, `BorderBrush`) a tonos neón retro-arcade (rosa neón, cian neón, negro tech profundo) de forma instantánea al hacer clic en "Aceptar", sin necesidad de reiniciar la app.
      - **Rediseño Completo de Colores (Bloques Cromáticos Interactivos)**: Reemplazados los cuadros de texto hexadecimales por botones rellenos con el color activo en tiempo real. Al pulsarlos, se despliega la nueva ventana modal independiente `ColorPickerDialog` para seleccionar colores mediante una paleta de 24 presets rápidos, deslizadores RGB analógicos o entrada de texto hexadecimal directa.
      - **Características**: Grupo de 7 checkboxes de control que regulan el coloreado visual de divisores, barras de desplazamiento y fondos difuminados.
      - **Fuentes**: Botones de selección tipográfica para personalizar fuentes de juego, barra lateral y detalles, acompañados de un botón para reestablecer valores por defecto.
    - **Activación y Vinculación Funcional de Categorías Avanzadas**:
      - **Depuración**: Activación y almacenamiento persistente de registros detallados (`AppSettings.EnableDebugLogs`).
      - **Notificaciones**: Selector de sistema de notificación (`AppSettings.NotificationSystem`) para alternar entre cuadros de mensaje y bandeja de sistema.
      - **Importaciones automatizadas**: Opción para habilitar el escaneo automático de ROMs en segundo plano (`AppSettings.EnableAutomaticRomImports`).
      - **Save Management (Gestión de Partidas Guardadas)**: Sistema modular completo con control de habilitación en cascada dinámico y reactivo en tiempo real para copias automáticas al cerrar, respaldos periódicos y límite de versiones configurable con control numérico.
      - **Aplicaciones de inicio**: Grid interactivo premium con agregación y remoción de programas mediante un selector asíncrono de ejecutables `.exe` (`OpenFilePickerAsync`) con parámetros y configuración de múltiples instancias.
      - **Bandeja de Sistema**: 4 checkboxes funcionales para regular el System Tray (minimizado, cerrado, notificaciones de bandeja) y el texto nativo de advertencia sobre las notificaciones de Windows.
      - **Reproducción de vídeo**: Selectores de tipo RadioButton agrupados para alternar el motor de vídeo preferido entre Windows Media Player y FFmpeg.
      - **Datos (Cabecera)**: Panel informativo nativo que guía al usuario a seleccionar una subcategoría a la izquierda.
      - **Copias de Seguridad (Datos)**: Checkbox funcional para habilitar respaldos automáticos de los archivos de metadatos XML de Biblioteca Externa.
      - **Game Progress Automation**: Autotrackeo completo de progreso de juegos a través de 9 condiciones (tiempo de juego en minutos, logros obtenidos, inactividad en días y logros de Softcore/Hardcore completados) vinculados a AppSettings con dropdowns de estado.
      - **Game Progress Organization**: Árbol jerárquico (`TreeView`) dinámico de grupos de progreso ("Not Started", "Active", "Done") con botones interactivos para reordenar (Subir/Bajar), agregar y eliminar grupos/estados con opción de reinicio por defecto.
      - **Prioridades de Región**: Lista interactiva de checkboxes bidireccionales con botones premium "▲ Subir" y "▼ Bajar" para reordenar dinámicamente y priorizar las regiones de metadatos.
      - **Buscar**: Ajustes de búsqueda avanzada de metadatos de Biblioteca Externa con checkboxes y NumericUpDown para el mínimo de calificaciones de la comunidad.
      - **Juegos Relacionados (Similares, Recomendados y Puertos)**: Tres subpaneles con tablas editables de reglas de afinidad de juegos en formato Grid (5 ComboBoxes y 1 TextBox por regla) con enlace de datos bidireccional y botones para revertir a las reglas predefinidas originales de Biblioteca Externa.
      - **RetroAchievements (Logros)**: Formulario de credenciales enmascaradas habilitado en cascada interactiva, incluyendo notificaciones y medallas visuales de logros, junto a un botón de testeo de conexión asíncrono con feedback visual inmediato.
    - Estructuración modular limpia que evita inflar la ventana principal y cumple rigurosamente con la **Política de Cero Advertencias**.

### v1.2.0-Dev (27 Mayo 2026)
*   **Sincronización de Base de Datos**: Propiedades extendidas (`Languages`, `AdditionalRoms`, `OverrideEmulatorPath`, `OverrideLaunchArguments` y `SelectedArtType`) ahora se persisten en la DB principal para evitar inconsistencias.
*   **Corrección de Importación**: Solucionados fallos críticos de traducción LINQ durante el escaneo de juegos mediante el uso de evaluación local (`AsEnumerable`).
*   **Registro de Errores**: Implementado log detallado en `import_error_log.txt` para diagnóstico preciso de fallos durante el proceso de importación.
*   **Alineación Biblioteca Externa**: Soporte total para metadatos avanzados, hardware de plataformas y nombres alternativos.
*   **Corrección de Calificación de Comunidad**: Solucionado el problema de solapamiento en el panel de detalles para evitar que etiquetas largas y valores amplios de calificación se superpongan en la UI (usando `DockPanel.Dock="Right"` y `TextTrimming="CharacterEllipsis"`). Además, se implementó el parseo y redondeo de la calificación a un decimal (ej. `3.6` en lugar de `3,6084742268041`), emulando perfectamente la estética de Biblioteca Externa, incorporando un fallback automático para que el indicador de estrellas y la calificación de la cabecera (bajo el banner) muestre de forma dinámica la calificación de la comunidad si el usuario aún no ha valorado el juego personalmente.
*   **Réplica Exacta de Metadatos de Biblioteca Externa**: Reestructurado por completo el bloque informativo para incluir exactamente las 12 columnas oficiales en el orden y denominación nativa de Biblioteca Externa, añadiendo soporte y lógica de cálculo dinámico para `Modo de Juego`, `Progress`, `Región`, `Estado`, `Portable`, `Fecha de Lanzamiento` y `Tipo de Lanzamiento`.
*   **Acciones y Diálogos Premium estilo Biblioteca Externa**: 
    - Implementados ToolTips interactivos detallados en el badge de estrellas de la cabecera (con desglose de calificación personal, promedio de comunidad a 2 decimales y votos totales) y en el botón de progreso rápido.
    - Se rediseñó el banner de acciones rápidas añadiendo un selector desplegable de estado (`BtnProgressQuick`) y un menú de más opciones (`•••`) con "Borrar" en color rojo.
    - Desarrollado el cuadro de diálogo de confirmación de borrado modal (`OverlayDeleteConfirm`) idéntico a Biblioteca Externa, que incluye un mensaje personalizado para cada juego, los botones "Yes" y "No" estilizados, y el checkbox funcional "Delete associated media" que elimina por completo los archivos extra e imágenes asociadas del juego de `GestorCovers.db`.





### v1.1.2.5-Dev (Actual)
*   **Réplica de Detalles de Plataforma y Categorías (Biblioteca Externa exacto)**: Reestructurado por completo el panel derecho de la aplicación para que al seleccionar una plataforma (como Amstrad CPC) o una categoría (como Computers) en el panel izquierdo, se visualicen sus estadísticas y metadatos con el mismo formato premium de Biblioteca Externa.
*   **Independencia Total de Biblioteca Externa**: Todos los datos técnicos de las plataformas (CPU, RAM, Gráficos, Sonido, Soporte de Carga, Desarrollador, Fabricante, Fecha de Estreno, Notas Históricas) y su foto física de hardware (`HardwareImage` binario) se almacenan de forma local en la base de datos `GestorJuegos.db` para que la aplicación funcione de forma autónoma.
*   **Importador de Metadatos y Consolas en Segundo Plano**: Potenciado `ImportBiblioteca ExternaAssets` para conectarse a la base de datos maestra `Biblioteca Externa.Metadata.db` localmente e importar todas las especificaciones y descripciones de las plataformas, así como leer las fotos de consolas físicas de la carpeta `Console` de Biblioteca Externa y guardarlas binariamente en SQLite.
*   **Estadísticas Dinámicas Agregadas**: Al navegar por categorías o plataformas en la barra lateral, se calculan automáticamente métricas locales de tu biblioteca (juegos totales, completados, veces jugados, tiempo total jugado, último juego jugado y juego más jugado).
*   **Auto-parcheo de Esquema SQLite**: Implementadas migraciones controladas en caliente para crear columnas técnicas y campos binarios en la tabla `Platforms` y el campo de descripción `Notes` en `PlatformCategories`.
*   **Rediseño de Rejilla de Juegos Estilo Biblioteca Externa**: Removido por completo el marco oscuro limitante alrededor de las carátulas y cambiado el modo de estiramiento a `Uniform`. La sombra se adapta ahora proporcional y exactamente al contorno y dimensiones reales de la carátula, flotando de forma inmersiva.
*   **Metadatos bajo la Portada**: El Título y el Desarrollador se posicionan de manera limpia e integrada bajo la imagen de portada, en consonancia directa con la interfaz de Biblioteca Externa.
*   **Estrategia de Fallback en Rejilla**: El subtexto evalúa la propiedad calculada `GridSubtext` (Desarrollador -> Distribuidor -> Año) de forma inteligente para rellenar de forma coherente las fichas de los juegos.
*   **Resplandor Azul Neón de Selección**: Añadido un efecto luminoso de glow (`#00a2ff`) alrededor del borde de la carátula o placeholder seleccionado para mejorar significativamente la interactividad de la UI.

### v1.1.2.4-Dev
*   **Barra Lateral Jerárquica (`TreeView`)**: Rediseño completo del panel izquierdo sustituyendo la lista plana por un árbol jerárquico al estilo Biblioteca Externa, agrupando las plataformas por categorías ("Computers", "Consoles", "Handhelds").
*   **Selector de Vista Dinámico**: Añadido un combobox superior para cambiar instantáneamente la vista de la barra lateral entre: Categoría de plataforma, Plataformas (lista plana), Géneros, Regiones y Biblioteca (Favoritos).
*   **Gestión y Sincronización de Iconos de Categoría**: Carga automática e importación a la base de datos de iconos pixel-art oficiales y Clear Logos de Biblioteca Externa para cada plataforma y categoría.
*   **Clasificación Inteligente Automática**: Reclasificación automatizada y persistente de las plataformas de "Consoles" a "Computers" o "Handhelds" según el nombre de la plataforma (ej. Game Boy, Amiga, Spectrum).
*   **Actualizaciones del Esquema en Caliente**: Implementado auto-parcheo en caliente del esquema de base de datos local SQLite al arrancar el programa, creando dinámicamente la tabla `PlatformCategories` y las columnas `Logo` e `Icon` si la base de datos ya existía, previniendo crashes.

### v1.1.2.3-Dev
*   **Rediseño de Detalles estilo Biblioteca Externa**: Reestructurado por completo el panel derecho de detalles para emular con fidelidad la experiencia premium de Biblioteca Externa (banner inmersivo, logotipo flotante transparente, calificación dinámica y estrellas celestes).
*   **Mejoras y Correcciones de Banner e Imágenes**:
    - **Solapamiento resuelto**: El banner/fanart del juego se mantiene visible debajo del logotipo del juego para una estética inmersiva real en lugar de un fondo plano, y se oculta el botón flotante "Ver todas las imágenes" cuando el juego dispone de logotipo, eliminando la superposición de texto y mejorando la legibilidad.
    - **Controlador de Captura**: Se solventó el bug de Avalonia XAML controlando programáticamente el placeholder "Sin captura de pantalla" desde el código de C#, eliminando la superposición del texto gray sobre las imágenes de fallback.
*   **Carrusel de Capturas Interactivo**: Añadido un visor de capturas grande acompañado de un carrusel horizontal de miniaturas para capturas adicionales y fanarts, con soporte para visor de pantalla completa inmersivo (`OverlayImageViewer`).
*   **Acciones Rápidas Flotantes**: Barra de herramientas flotante con accesos directos rápidos para Editar, Favorito rápido (corazón dinámico que cambia de color e icono en tiempo real), Abrir Carpeta y Eliminar.
*   **Botón JUGAR Multi-Disco Inteligente**: Reemplazado por un botón verde gigante con degradado premium y un menú desplegable dinámico que detecta automáticamente los discos/ROMs en juegos multi-disco para iniciarlos individualmente.
*   **Fichas de Datos Técnicos e Internet**: Cuadrícula de detalles técnicos estilo tarjeta (Clasificación, Género, Series, Modo, Progreso coloreado, Estado, Archivo) e iconos de acceso rápido para buscar trailers en YouTube y artículos en Wikipedia.

### v1.1.2.2-Dev
*   **Navegación Sonora Inmersiva**: Los efectos de sonido ahora se reproducen al pasar el ratón por las listas de juegos y plataformas, no solo al hacer clic.
*   **Optimización de Carga Crítica**: Eliminado el retardo al abrir plataformas mediante una precarga más eficiente de la base de datos y paginación optimizada.
*   **Sonido de Alta Velocidad**: Implementado sistema de caché en memoria para los sonidos (`SoundHelper`), eliminando latencias de disco durante la navegación rápida.
*   **Compilación Portable**: La carpeta de sonidos ahora se incluye automáticamente en el directorio de salida para facilitar la creación de instaladores.

### v1.1.2.1-Dev
*   **Configuración de Audio**: Añadida opción en los ajustes para activar/desactivar los efectos de sonido (SFX).
*   **Detalles Extendidos en UI**: El panel de información ahora muestra Desarrollador, Distribuidor y Descripción del juego, con visibilidad dinámica.
*   **Refactorización y Estilos**: Centralización de recursos visuales y creación de estilos compartidos para botones y encabezados, mejorando la coherencia visual.

### v1.1.2.0-Dev
*   **Rediseño de Formulario de Edición (Estilo Playnite)**: El `OverlayEditGame` ha sido completamente rediseñado con una estructura de pestañas profesional:
    - **Metadatos**: Nuevos campos para Desarrollador, Distribuidor, Región (con selector visual) y Géneros.
    - **Archivos**: Gestión centralizada de ROMs, emuladores personalizados y argumentos de lanzamiento.
    - **Multimedia**: Selector de tipo de arte preferido por juego y previsualización de carátulas.
    - **Descripción**: Nuevo campo de texto multilínea para historias y detalles del juego.
*   **Expansión del Modelo de Datos**: Actualización de la base de datos principal para soportar los nuevos campos de metadatos mediante migraciones automáticas.
*   **Sincronización de Arte Mejorada**: El tipo de arte seleccionado en el formulario de edición se sincroniza instantáneamente con la vista de la biblioteca.

### v1.1.1.0-Dev
*   **Autonomía de Multimedia**: Migración completa de la visualización de imágenes a la base de datos local. La aplicación ya no requiere acceso a la carpeta de Biblioteca Externa para mostrar logotipos o fondos de juegos ya importados.
*   **Gestión de ExtraImages**: `GameService` actualizado para soportar el almacenamiento y actualización de múltiples tipos de arte por juego (Clear Logo, Fanart, Screenshot, etc.).
*   **Importador Biblioteca Externa Potenciado**: Ahora captura automáticamente hasta 15 tipos diferentes de arte multimedia durante la importación inicial.
*   **Corrección de Estabilidad**: Solucionados errores de compilación relacionados con directivas de espacio de nombres y propiedades de XAML no compatibles.

### v1.1.0.8
*   **Corrección Visual Total**: Implementación de `ItemTemplate` en todas las listas del sistema, eliminando la aparición de nombres de clase técnicos.
*   **Muro de Plataformas Rediseñado**: Nueva cuadrícula estética con logos y tipografía mejorada para una selección de sistemas más intuitiva.
*   **Overlays Pulidos**: Mejora visual en los paneles de edición de juegos, gestión de plataformas y búsqueda IGDB/Vimm.
*   **Búsqueda Global Mejorada**: Los resultados ahora muestran la plataforma y mantienen la coherencia visual con la biblioteca.

### v1.1.0.7
*   **Limpieza de Dashboard**: Se ha simplificado el panel central para que sea una pantalla de bienvenida limpia, moviendo toda la lógica de datos a overlays.
*   **Overlay de Estadísticas Profesional**: Implementación de plantillas visuales en `OverlayFullStats`. Ahora muestra banderas, iconos de plataforma y contadores estilizados.
*   **Consolidación de C#**: Limpieza de métodos y eventos obsoletos vinculados a controles eliminados del Dashboard.

### v1.1.0.5
*   **Rediseño de Interfaz (3 Paneles)**:
    - Izquierda: Explorador de plataformas mejorado.
    - Centro: Rejilla dedicada exclusivamente a la visualización de carátulas.
    - Derecha: Panel de información detallada (informativo, no intrusivo).
*   **Barra Superior de Acciones**: Los botones "Añadir Juego", "Estadísticas" y "Gestionar" se han movido a la barra superior para liberar espacio.
*   **Ventanas Modales (Overlays)**: Las estadísticas y la creación/edición de juegos ahora se gestionan mediante overlays centralizados.
*   **Corrección de Errores**: Solucionado el crash crítico de recursos al navegar por consolas (`Static resource ByteToBitmap not found`).
*   **Estabilidad**: Limpieza de referencias obsoletas y optimización del arranque.

### v1.1.0.4
*   **Corrección Crítica de Carátulas**: Implementación de `INotifyPropertyChanged` en el modelo `Game` para asegurar el refresco de miniaturas al navegar por el árbol.
*   **Gestión de Categorías**: 
    - Selector de categorías (Consolas, Portátiles, Ordenadores, Arcade) en diálogos de creación y gestión.
    - Motor de detección automática de categorías basado en nombres de plataforma.
    - Integración total en importaciones de Biblioteca Externa, Drag & Drop y escaneo de carpetas.
*   **Migración de DB**: Sistema de auto-parche para asegurar la columna `Category` en instalaciones existentes.

### v1.1.0.3
*   **Layout Desktop (estilo Biblioteca Externa)**: Nueva rejilla central de 3 columnas con árbol lateral y panel de detalles.
*   **Categorización de Plataformas**: Las plataformas ahora se agrupan por categorías (Computers, Consoles, Handhelds) en el árbol lateral.
*   **Migración de DB Automática**: Script de emergencia para añadir la columna `Category` a la tabla `Platforms`.

### v1.1.0.2
- **Grupos de Imagen Biblioteca Externa**: Implementado el sistema de organización de imágenes idéntico a Biblioteca Externa (Background, 3D Boxes, Marquee, etc.).
- **Búsqueda Inteligente de Multimedia**: El sistema mapea automáticamente los nombres amigables a las carpetas físicas de Biblioteca Externa.
- **Mejoras en Importación**: Corregidas las rutas de escaneo para carátulas de juegos, asegurando que se encuentren en la estructura estándar de Biblioteca Externa.

### v1.1.0.1
- **Selector de Tipo de Arte**: Añadido menú desplegable para alternar carátulas en tiempo real.
- **Búsqueda Flexible**: Soporte para encontrar carátulas por prefijo (ej: "Sonic (USA)" coincide con "Sonic").

### v1.0.9.9
- **Eliminación de Integración EmuMovies**: Retirada del soporte para la API de EmuMovies para favorecer el uso de artes locales y herramientas externas.
- **Limpieza de Interfaz**: Eliminación de credenciales y campos de búsqueda redundantes.

### v1.0.9.8
- **Optimización Crítica de Rendimiento**: Implementación de inserciones y actualizaciones por lotes (Batch Insert) en todos los importadores.
- **Configuración Global Persistente**: Nuevo panel de configuración para gestionar rutas de Biblioteca Externa, preferencias de arte y credenciales de EmuMovies.
- **Integración Avanzada con Biblioteca Externa**: Importación automática de carátulas locales (Box Front, 3D, etc.) durante el escaneo de plataformas.
- **Selector Dinámico de Arte**: Posibilidad de alternar entre diferentes tipos de imágenes locales desde el panel de detalles.
- **Refuerzo de Arquitectura Dual**: Eliminación de datos multimedia redundantes de la base de datos principal y uso de `[NotMapped]` para mayor integridad.

### v1.0.9.7
- **Persistencia de Configuración**: La ruta de Biblioteca Externa se guarda en `appsettings.json` tras la primera selección.
- **UX de Importación**: Validación inteligente de carpetas de Biblioteca Externa para asegurar instalaciones válidas.

### v1.0.9.6
- **Importador Nativo Biblioteca Externa**: Lectura directa de XML de plataformas con extracción de metadatos (Géneros, Años, Rutas, Favoritos).
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
Desarrollado con ❤️ por Scorpio.
