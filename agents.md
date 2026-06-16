# Constitución de los Agentes - Gestor de Juegos (v2.0)

## 🛡️ Protocolo de Seguridad y Estabilidad

1. **Transformaciones XAML/C#**: Siempre utilizar archivos temporales `.tmp` antes de sobrescribir archivos críticos.
2. **Validación de Compilación**: Ejecutar `dotnet build` tras cada cambio en componentes modulares.
3. **Arquitectura de UI**: Prohibido editar `MainWindow` para añadir lógica pesada. Todo panel nuevo debe ser un `UserControl`.

## 📂 Jerarquía de Carpetas (Estructura v6.0)

Todos los componentes de interfaz DEBEN residir en su carpeta correspondiente dentro de `GestorJuegos/Views/`:

- `Windows/`: Ventanas completas (Window).
- `Overlays/`: Paneles flotantes con fondo semitransparente que cubren la UI.
- `Panels/`: Componentes fijos integrados en el layout principal.
- `Items/`: Plantillas de datos y elementos de lista repetibles.
- **Prohibido**: Añadir archivos `.axaml` directamente en la raíz del proyecto.

## 📏 Reglas Estrictas de Diseño (Reto 300-400)

1. **MainWindow.axaml.cs**: Debe mantenerse estrictamente entre **300 y 400 líneas**.
    - Si sube de 400, se debe extraer lógica a un nuevo componente o servicio.
    - Si baja de 300, verificar si se ha perdido legibilidad o funcionalidad esencial.

2. **Funciones y Clases**:
    - Máximo 50-60 líneas por método.
    - Una única responsabilidad por componente.
3. **Eventos de UI**: Preferir **expresiones lambda** en `SetupEvents` para acciones simples (cierre de overlays, toggles, mensajes) para mantener el archivo principal limpio.

## 🧱 Componentes Modulares de Referencia

Cualquier modificación debe respetar la estructura de los componentes extraídos en la Fase 6:

- `TopBarView`: Gestión de menús y búsqueda.
- `SidebarView`: Árbol de navegación y filtros.
- `LibraryView`: Galería central (Grid, List, Ruedas).
- `GameDetailsView`: Panel de información, 3D y logros.
- `DashboardView`: Pantalla de bienvenida y detalles técnicos de plataformas.
- `BackgroundView`: Sistema de fondos inmersivos y dinámicos.

## 🧠 Flujo de Trabajo Técnico

- **Inyección de Servicios**: Los componentes deben recibir `GameService` o `ScannerService` para ser autónomos.
- **Comunicación**: Usar eventos (`EventHandler`) para que los hijos informen al padre (`MainWindow`) de acciones globales.
- **Limpieza**: No dejar métodos vacíos o comentarios de código muerto. Saneamiento inmediato tras modularización.

## 🚀 Pautas de Comunicación

- Responder siempre en español.
- Tono profesional, directo y centrado en la arquitectura modular.
- Explicar siempre el *porqué* de una extracción de código.

## 📋 README.md

Has un readme profesional y completo del proyecto, explicando la estructura.
Usa el idioma español.

- No necesito que vayas poniendo códigos de ejemplo, ya se que funciona, solo necesito que - expliques como funciona.
- Los botones deben ser fáciles de encontrar y usar
- No necesito que vayas poniendo las mejoras de las versiones.
- Usa un menu clikearble para ir navegando sobre el contenido de cada seccion en lugar de usar scrollbars.
