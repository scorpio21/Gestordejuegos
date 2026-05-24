# ⚠️ Preferencias de Desarrollo: Política de Cero Advertencias

Este archivo almacena de forma persistente las pautas y preferencias clave del usuario para que cualquier desarrollador o agente de inteligencia artificial que trabaje en esta base de código las siga estrictamente:

## 🚫 Política de Cero Advertencias (Zero Warnings Policy)
* **Al usuario NO le gusta bajo ningún concepto que aparezcan advertencias (warnings) durante la compilación.**
* Todo código desarrollado, modificado o refactorizado **DEBE compilar obligatoriamente con 0 advertencias y 0 errores** (`0 Advertencia(s), 0 Errores`).
* Siempre que se realicen cambios, se debe ejecutar `dotnet build` y solucionar de inmediato:
  * **Advertencias Nullable (`CS8600`, `CS8602`)**: Usar tipos nullables (`string?`), comprobaciones de tipo (`is ComboBoxItem item`) o aserciones de no-nulo de forma limpia.
  * **Advertencias de Asincronía (`CS1998`)**: Remover la palabra clave `async` en tareas CPU-bound síncronas que corran en `Task.Run` o implementar `await` adecuadamente si es necesario.
  * **Advertencias de Compatibilidad (`CA1416`)**: Dado que la aplicación está orientada y optimizada para ejecutarse en Windows, se deben suprimir las advertencias de APIs específicas de Windows (como `SoundPlayer`) usando la directiva `#pragma warning disable CA1416` al inicio de los ficheros de utilidad correspondientes.
