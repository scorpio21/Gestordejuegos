# Constitución de los Agentes - Gestor de Juegos

## 🛡️ Protocolo de Seguridad y Estabilidad
1.  **Transformaciones XAML/C#**: Siempre utilizar archivos temporales `.tmp` antes de sobrescribir archivos críticos. Nunca realizar cambios masivos directamente en archivos de UI sin verificación previa.
2.  **Validación de Compilación**: Cada cambio atómico debe ser seguido por un `dotnet build` para asegurar la integridad del sistema.
3.  **Gestión de Errores**: Si un cambio causa errores de compilación, revertir inmediatamente al estado anterior antes de intentar una nueva estrategia.

## 📏 Reglas Estrictas de Codificación (Obligatorias)
1.  **Límites de Archivo**: 
    - Ningún archivo puede superar las **300 líneas**.
    - Ninguna función puede superar las **60 líneas**.
    - Máximo **una clase por archivo**.
2.  **Modularización Automática**:
    - Si un archivo supera los límites establecidos, **DEBE** ser dividido automáticamente en componentes o servicios más pequeños.
    - Cada panel complejo o bloque de UI con más de 150 líneas debe convertirse en un `UserControl` independiente en `/Controls` o carpetas específicas.
3.  **Arquitectura Avalonia**:
    - `MainWindow.axaml` solo debe contener el layout principal, contenedores y referencias a otros controles.
    - Prohibido tener bloques gigantes de UI o múltiples responsabilidades en un solo archivo.
    - Seguir patrón MVVM: `Views`, `ViewModels`, `Models`, `Services`, `Resources`, `Utils`, `Controls`.
4.  **Orden del Código**:
    1. Imports/Usings
    2. Constantes
    3. Clases
    4. Funciones
    5. Ejecución principal/Lógica

## 🧠 Flujo de Trabajo
*   **Investigación**: Leer el archivo antes de editarlo y verificar patrones existentes.
*   **Modularización**: Priorizar la extracción de lógica a servicios y UI a controles independientes.
*   **Documentación**: Documentar cada función pública y explicar cambios estructurales.
*   **Sincronización**: Realizar commits frecuentes y mantener el `.gitignore` actualizado.

## 🚀 Pautas de Comunicación
1. Sé conversacional pero profesional.
2. Refiérete al USUARIO en segunda persona y a ti mismo en primera persona.
3. Formatea las respuestas en Markdown usando backticks para símbolos técnicos.
4. Evita disculpas innecesarias; explica el error técnico y la solución.
