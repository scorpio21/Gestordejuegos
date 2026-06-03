# 🎨 Instrucciones de Integración de Temas

Este documento explica cómo funciona el sistema de temas y cómo interactúan la aplicación principal (**GestorJuegos**) y la herramienta independiente (**CreadorTemas**).

---

## 🛠️ Cómo Compilar y Ejecutar el Creador de Temas

El Creador de Temas es una aplicación independiente de Avalonia UI desarrollada con .NET. Para ejecutarla:

1. Abre una consola de comandos en la carpeta de la herramienta:
   ```bash
   cd CreadorTemas
   ```
2. Ejecuta la aplicación usando el SDK de .NET:
   ```bash
   dotnet run
   ```

---

## 🔗 Integración Sin Modificaciones de Código

La aplicación principal (`GestorJuegos`) lee dinámicamente todos los temas disponibles escaneando la carpeta `Themes` de su directorio de ejecución. 

### ¿Cómo detecta los temas la aplicación principal?
Cuando abres la ventana de Opciones en `GestorJuegos`, la aplicación:
1. Escanea las subcarpetas del directorio `Themes/`.
2. Verifica que cada subcarpeta contenga un archivo válido llamado `theme.json`.
3. Si existe, agrega el nombre de la subcarpeta como una opción seleccionable en el selector de temas.

### ¿Cómo conecta el Creador de Temas con la aplicación?
Al iniciar el **Creador de Temas**, verás un campo en la parte superior donde debes indicar la ruta del directorio `Themes` de la aplicación principal. 
- Si estás desarrollando, la ruta por defecto será: `../GestorJuegos/Themes` o la ruta correspondiente dentro de la compilación local (`../GestorJuegos/bin/Debug/net9.0/Themes`).
- Si utilizas una versión publicada, simplemente selecciona la carpeta `Themes/` dentro del directorio donde tengas instalado el `GestorJuegos`.

Al presionar **Guardar Tema** en el creador, este copiará automáticamente todas las fuentes, imágenes de fondo y generará el archivo `theme.json` correspondiente en dicha carpeta. La aplicación principal detectará el nuevo tema inmediatamente la próxima vez que cargue la lista o al arrancar.

---

## 📄 Estructura del Archivo de Tema (`theme.json`)

Cada tema guardado sigue este esquema JSON estructurado:

```json
{
  "Colors": {
    "AccentBrush": "#ff007f",
    "DeepDarkBrush": "#0b0c10",
    "PanelBrush": "#161920",
    "BorderBrush": "#00f3ff",
    "MainForeground": "#ffffff",
    "SecondaryTextBrush": "#00f3ff"
  },
  "Fonts": {
    "MainFont": "Beon.ttf",
    "HeaderFont": "Kimberley.otf"
  },
  "BackgroundImage": "Images/Background-01.jpg",
  "OverlayImage": "Images/Overlay.png",
  "Metrics": {
    "CornerRadius": "8"
  },
  "PreferredView": "Grid"
}
```

- **Colors**: Define los pinceles dinámicos de colores en formato hexadecimal.
- **Fonts**: Nombres de los archivos de fuentes locales copiados al tema.
- **BackgroundImage / OverlayImage**: Rutas relativas a las imágenes de fondo y superposiciones.
- **Metrics**: Valores numéricos como el radio de bordes de las tarjetas de juego.
- **PreferredView**: Tipo de vista por defecto preferida para el tema.
