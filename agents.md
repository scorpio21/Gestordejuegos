# Constitución de los Agentes - Gestor de Juegos

## 🛡️ Protocolo de Seguridad y Estabilidad
1.  **Transformaciones XAML/C#**: Siempre utilizar archivos temporales `.tmp` antes de sobrescribir archivos críticos. Nunca realizar redirecciones de entrada/salida sobre el mismo archivo.
2.  **Validación de Compilación**: Tras cada cambio estructural (movimiento de controles, cambio de namespaces), ejecutar `dotnet build`.
3.  **Modularización**: Evitar archivos de más de 300 líneas. Si un componente crece, extraerlo a un `UserControl` en `/Controls` o en la raíz si es una "View" mayor.

## 🏗️ Arquitectura de UI
*   **Estilos**: Centralizados en `Styles.axaml`.
*   **Overlays**: Cada panel pesado (Estadísticas, Editores) debe ser un `UserControl` independiente con su propia lógica encapsulada.
*   **Comunicación**: Los controles hijos se comunican con el padre mediante eventos (`EventHandler`) y métodos de inicialización (`Initialize`).

## 🧠 Flujo de Trabajo
*   **Fase 1**: Extracción de Recursos.
*   **Fase 2**: Modularización de Componentes.
*   **Fase 3**: Refactorización de Lógica.
