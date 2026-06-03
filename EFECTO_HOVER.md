# 🌟 Guía de Modificación para Efecto Hover y Selección Dinámicos y Personalizados

Esta guía detalla la integración del **Efecto Hover** (Color de borde y desenfoque del brillo) y del **Logotipo de la App** en la aplicación principal (`GestorJuegos`), sincronizándolo con los temas diseñados en el **Creador de Temas**.

---

## 📝 Paso 1: Modificaciones en `MainWindow.axaml` (GestorJuegos) - **[HECHO (ECHO)]**

Los estilos dentro de la interfaz gráfica en tu `MainWindow.axaml` han sido adaptados con éxito para consumir los recursos dinámicos `HoverBorderBrush` y `HoverBoxShadow` en todas las vistas de juego:

### 1.1 Estilos al pasar el ratón (Hover) en carátulas - **[HECHO (ECHO)]**
Las reglas responden al color y la sombra dinámica del tema activo:
```xml
<!-- Efecto de Hover para carátulas reales -->
<Style Selector="ListBoxItem:pointerover Border#CoverBorder">
    <Setter Property="BorderBrush" Value="{DynamicResource HoverBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
    <Setter Property="BoxShadow" Value="{DynamicResource HoverBoxShadow}"/>
</Style>
<!-- Efecto de Hover para placeholders de juegos sin carátula -->
<Style Selector="ListBoxItem:pointerover Border#PlaceholderBorder">
    <Setter Property="BorderBrush" Value="{DynamicResource HoverBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
    <Setter Property="BoxShadow" Value="{DynamicResource HoverBoxShadow}"/>
</Style>
```

### 1.2 Estilos al pasar el ratón (Hover) en vista de lista - **[HECHO (ECHO)]**
```xml
<!-- Cambia sutilmente el borde del contenedor al color del hover del tema -->
<Style Selector="ListBoxItem:pointerover Border">
    <Setter Property="BorderBrush" Value="{DynamicResource HoverBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>
```

### 1.3 Efecto Hover en la Vista de Rueda Vertical (`LstGamesWheelVertical`) - **[HECHO (ECHO)]**
Se han añadido estas reglas dentro del bloque `<ListBox.Styles>` de `LstGamesWheelVertical`:
```xml
<!-- Aumentar opacidad del elemento de la rueda al pasar el ratón -->
<Style Selector="ListBoxItem:pointerover">
    <Setter Property="Opacity" Value="0.9"/>
</Style>
<!-- Iluminar el texto del juego con el color de hover del tema -->
<Style Selector="ListBoxItem:pointerover TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource HoverBorderBrush}"/>
</Style>
```

### 1.4 Efecto Hover en la Vista de Rueda Horizontal / Coverflow (`LstGamesWheelHorizontal`) - **[HECHO (ECHO)]**
Se han añadido estas reglas dentro del bloque `<ListBox.Styles>` de `LstGamesWheelHorizontal`:
```xml
<!-- Aumentar opacidad de la carátula al pasar el ratón -->
<Style Selector="ListBoxItem:pointerover">
    <Setter Property="Opacity" Value="0.9"/>
</Style>
<!-- Añadir borde con el color de hover del tema al contenedor de la carátula -->
<Style Selector="ListBoxItem:pointerover Border">
    <Setter Property="BorderBrush" Value="{DynamicResource HoverBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>
```

---

## 💻 Paso 2: Modificaciones en `MainWindow.axaml.cs` (GestorJuegos) - **[HECHO (ECHO)]**

Se ha integrado con éxito la inicialización y carga de los recursos dinámicos en C# para garantizar compatibilidad retroactiva y evitar excepciones:

### 2.1 Carga Dinámica al leer el JSON del tema - **[HECHO (ECHO)]**
La aplicación principal evalúa los campos `HoverBorderBrush` y `HoverGlowBlur` de `theme.json` al aplicar el tema seleccionado:
```csharp
                                // 8. Carga Dinámica de Efectos Hover
                                if (themeConfig != null)
                                {
                                    // Determinar el color del hover (si no está definido, se usa el color de acento)
                                    if (!this.Resources.ContainsKey("HoverBorderBrush"))
                                    {
                                        this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
                                    }

                                    // Determinar el desenfoque de brillo hover (HoverGlowBlur)
                                    double glowBlur = 12; // Valor por defecto
                                    if (themeConfig.Metrics != null && themeConfig.Metrics.TryGetValue("HoverGlowBlur", out string? glowStr) && double.TryParse(glowStr, out double gb))
                                    {
                                        glowBlur = gb;
                                    }

                                    var hoverBrush = (Avalonia.Media.SolidColorBrush)this.Resources["HoverBorderBrush"];
                                    if (glowBlur > 0)
                                    {
                                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                                        {
                                            Blur = glowBlur,
                                            Spread = 2,
                                            Color = hoverBrush.Color,
                                            OffsetY = 0
                                        });
                                    }
                                    else
                                    {
                                        // Brillo desactivado (sombra transparente)
                                        this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
                                        {
                                            Blur = 0,
                                            Spread = 0,
                                            Color = Avalonia.Media.Colors.Transparent,
                                            OffsetY = 0
                                        });
                                    }
                                }
```

### 2.2 Fallbacks de Hover y temas por defecto - **[HECHO (ECHO)]**
Tanto en la inicialización sin tema como en los bloques de temas integrados (`Old Default`, `Personalizado` y fallbacks en bloque `catch`), se crean siempre las propiedades correspondientes:
```csharp
            this.Resources["HoverBorderBrush"] = this.Resources["AccentBrush"];
            this.Resources["HoverBoxShadow"] = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow
            {
                Blur = 12,
                Spread = 2,
                Color = accentColor, // o defaultAccent.Color
                OffsetY = 0
            });
```

---

## 🎨 Integración y Guardado en el Creador de Temas - **[HECHO (ECHO)]**

El **Creador de Temas** tiene implementados todos los controles y está operativo:
1. **Logotipo de la App**: Selecciona una imagen personalizada que se copiará como `Images/Logo.png` en la carpeta del tema. **[HECHO]**
2. **Color de Borde Hover**: Escribe el valor seleccionado bajo la clave `HoverBorderBrush`. **[HECHO]**
3. **Desenfoque de Brillo Hover**: Escribe el valor numérico bajo el parámetro de métricas `HoverGlowBlur`. **[HECHO]**
4. **Vista Previa en Vivo**: Renderiza el logotipo de forma interactiva y simula el color y brillo del hover al pasar el ratón por la tarjeta. **[HECHO]**
