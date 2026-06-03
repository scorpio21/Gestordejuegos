# 🌟 Guía de Modificación para Efecto Hover y Selección Dinámicos

Para que la aplicación principal (`GestorJuegos`) muestre efectos de selección y pase de ratón (hover) adaptados al color de acento (`AccentBrush`) del tema activo, sigue estos sencillos pasos para actualizar el diseño.

---

## 📝 Modificaciones en `MainWindow.axaml`

Abre el archivo [MainWindow.axaml](file:///k:/GestorJuegos/GestorJuegos/MainWindow.axaml) en Visual Studio y realiza las siguientes modificaciones de estilos:

### Paso 1: Resplandor Temático en Selección (Cuadrícula de Juegos)

Busca la etiqueta `<ListBox Name="LstGamesGrid" ...>` (alrededor de la línea 637) y desplázate hacia abajo hasta sus estilos internos `<ListBox.Styles>`. Reemplaza los estilos de selección para que utilicen el recurso dinámico del tema (`AccentBrush`):

**Código Anterior (Líneas 691 - 703):**
```xml
<Style Selector="ListBoxItem:selected /template/ ContentPresenter">
    <Setter Property="Background" Value="Transparent"/>
</Style>
<Style Selector="ListBoxItem:selected Border#CoverBorder">
    <Setter Property="BoxShadow" Value="0 0 15 2 #00a2ff"/>
    <Setter Property="BorderBrush" Value="#00a2ff"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>
<Style Selector="ListBoxItem:selected Border#PlaceholderBorder">
    <Setter Property="BoxShadow" Value="0 0 15 2 #00a2ff"/>
    <Setter Property="BorderBrush" Value="#00a2ff"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>
```

**Código Nuevo (Modificado):**
```xml
<Style Selector="ListBoxItem:selected /template/ ContentPresenter">
    <Setter Property="Background" Value="Transparent"/>
</Style>
<Style Selector="ListBoxItem:selected Border#CoverBorder">
    <!-- El borde ahora se ilumina dinámicamente con el color de acento del tema -->
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>
<Style Selector="ListBoxItem:selected Border#PlaceholderBorder">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>
```

---

### Paso 2: Efecto Hover Temático al pasar el Ratón (Cuadrícula de Juegos)

Justo debajo de los estilos anteriores (dentro de los mismos `<ListBox.Styles>` de `LstGamesGrid`), añade las siguientes reglas para mostrar un sutil borde del color de acento del tema activo cuando el puntero del ratón se deslice sobre la carátula de un juego:

**Código a Añadir:**
```xml
<!-- Efecto de Hover para carátulas reales -->
<Style Selector="ListBoxItem:pointerover Border#CoverBorder">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>
<!-- Efecto de Hover para placeholders de juegos sin carátula -->
<Style Selector="ListBoxItem:pointerover Border#PlaceholderBorder">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>
```

---

### Paso 3: Efecto Hover Temático en la Vista de Lista

Si deseas que la vista de lista clásica también tenga un comportamiento dinámico, busca la etiqueta `<ListBox Name="LstGames" ...>` (alrededor de la línea 586), ve a su bloque `<ListBox.Styles>` y añade el siguiente estilo:

**Código a Añadir:**
```xml
<!-- Cambia sutilmente el borde del contenedor del juego al color de acento al pasar el ratón -->
<Style Selector="ListBoxItem:pointerover Border">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>
```

---

## 🎨 ¿Cómo se integra con el Creador de Temas?

Al guardar un tema en el **Creador de Temas**, el valor del color que elijas en el selector gráfico para **Acento (AccentBrush)** se escribirá directamente en el archivo `theme.json` de ese tema.

Gracias a los cambios anteriores en `MainWindow.axaml`, cuando cambies de tema en las opciones del Gestor de Juegos, Avalonia UI actualizará en caliente todos los efectos de selección y hover utilizando la nueva paleta de color del tema de forma inmediata.
