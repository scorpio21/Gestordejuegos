# Guía Técnica: Implementación de Cajas 3D (Estilo Biblioteca Externa)

Este documento detalla cómo funciona el renderizado de cajas de juegos en sistemas como Biblioteca Externa y cómo implementarlo de forma profesional en AvaloniaUI.

## Cómo funciona Biblioteca Externa
Biblioteca Externa no utiliza un motor de videojuegos pesado; en su lugar, emplea una técnica de mapeo de texturas sobre un sólido geométrico simple:
1. **Modelo**: Un cuboide (caja) con proporciones de estuche de juego.
2. **Texturas**: Renderiza 6 imágenes (frontal, trasera, lateral izquierdo/derecho, superior/inferior).
3. **Efectos**: Aplica rotación con suavizado (easing), iluminación básica, sombras dinámicas y reflejos en el suelo.
4. **Interacción**: Arrastre con el ratón para rotar y rueda para zoom.

## Opciones de implementación en AvaloniaUI

### Opción 1: Avalonia 3D SceneGraph (RECOMENDADA)
Avalonia (versiones 11/12+) permite trabajar con un grafo de escena 3D experimental.
- **Ventajas**: Aceleración por GPU, soporte real para luces, materiales y cámaras.
- **Arquitectura**:
  ```xml
  <SceneView>
      <Scene>
          <PerspectiveCamera Position="0,0,3" LookDirection="0,0,-1" />
          <DirectionalLight Direction="-1,-1,-1" />
          <MeshNode Name="BoxNode" />
      </Scene>
  </SceneView>
  ```

### Opción 2: Pseudo-3D (Simulación con Matrix3D)
Es lo que estamos usando actualmente pero requiere mayor precisión matemática.
- **Ventajas**: Muy ligero, no requiere módulos experimentales.
- **Desventajas**: No hay luces reales ni profundidad de oclusión automática.

### Opción 3: Motor 3D Externo (Stride / OpenTK)
Para proyectos que requieran realismo extremo (PBR, sombras suaves, shaders personalizados).
- **Ventajas**: Potencia total de un motor de juegos.
- **Desventajas**: Mayor complejidad de integración y peso del binario.

## Ejemplo de implementación (SceneGraph)

### Código C# (Lógica del Visor)
```csharp
public void LoadBox(Bitmap frontTexture)
{
    // Crear el cuboide con proporciones de caja de juego (Ancho, Alto, Grosor)
    var mesh = MeshBuilder.CreateBox(1.0, 1.4, 0.25);

    var material = new StandardMaterial
    {
        Diffuse = new TextureBrush(frontTexture),
        SpecularPower = 32
    };

    BoxNode.Mesh = mesh;
    BoxNode.Material = material;
}

public void UpdateRotation(double rotX, double rotY)
{
    var matrixX = Matrix4x4.CreateRotationX(MathF.ToRadians((float)rotX));
    var matrixY = Matrix4x4.CreateRotationY(MathF.ToRadians((float)rotY));
    BoxNode.Transform = matrixX * matrixY;
}
```

### Interacción con Mouse (Drag & Zoom)
```csharp
public void OnPointerMoved(object? s, PointerEventArgs e)
{
    if (!_dragging) return;
    var delta = e.GetPosition(null) - _lastPos;
    RotationY += delta.X * 0.3;
    RotationX += delta.Y * 0.3;
    UpdateRotation(RotationX, RotationY);
}
```

## Conclusión para GestorJuegos
Para este proyecto, la **Opción 1 (SceneGraph)** es la evolución lógica. Permite mantener la ligereza de Avalonia mientras ofrece un visor de carátulas profesional, sólido y con iluminación real que no se deforma al rotar.
