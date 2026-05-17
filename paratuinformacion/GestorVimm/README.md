# Descarga de carátulas (GestorVimm)

Aplicación de escritorio para **Windows, Linux y macOS** que descarga carátulas (box art) desde [Vimm's Lair – The Vault](https://vimm.net/vault) a partir de un listado de ROMs en un fichero `.txt`.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-12-7B2CBF)

## Características

- Selección de **plataforma** (NES, SNES, N64, 3DS, PS1, etc.).
- Carga del listado por **fichero .txt**, **arrastrar y soltar** o ruta manual.
- Análisis de nombres en formato **No-Intro** (región e idiomas).
- Descarga masiva de carátulas en **PNG**.
- **Omite** juegos cuya carátula ya existe en la carpeta de salida.
- **Barra de progreso** con contador.
- **Vista previa** de carátulas (clic en la lista o navegación anterior/siguiente).
- **Cancelar** descarga en cualquier momento.
- Pausa entre peticiones para no saturar el servidor de Vimm.

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)

## Uso

1. Clona el repositorio y entra en la carpeta del proyecto.
2. Ejecuta la aplicación:

   ```bash
   dotnet run
   ```

3. Elige la **plataforma** en el desplegable.
4. Carga un **.txt** (un juego por línea) o arrástralo a la zona indicada.
5. Pulsa **Analizar** para ver el listado parseado.
6. Indica la **carpeta de salida** y pulsa **Descargar carátulas**.

### Formato del fichero .txt

Una línea por ROM. Ejemplo:

```text
Sushi Striker - The Way of Sushido (Europe) (En,Fr,De,Es,It,Nl).zip
1080 Snowboarding (Europe) (En,Ja,Fr,De).z64
The Legend of Zelda - Ocarina of Time (Europe) (En,Fr,De).z64
```

Se interpreta así:

| Parte | Ejemplo |
|--------|---------|
| Nombre | `Sushi Striker - The Way of Sushido` |
| Región | `Europe` |
| Idiomas | `En,Fr,De,Es,It,Nl` |

También vale sin idiomas: `Mi Juego (USA).cia`

### Compilar ejecutable

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

El binario quedará en `bin/Release/net9.0/win-x64/publish/`.

## Notas

- Las carátulas provienen del Vault de Vimm.net (datos de [GameTDB](https://www.gametdb.com)).
- Se necesita conexión a Internet.
- Respeta los términos de uso de [vimm.net](https://vimm.net/).
- Si un juego no aparece en el Vault o el nombre no coincide, no se descargará carátula (se indica en el log).

## Estructura del proyecto

```
GestorVimm/
├── Models/              # RomEntry
├── Services/            # Parser de nombres y cliente Vimm
├── MainWindow.axaml     # Interfaz
├── MainWindowViewModel.cs
└── GestorVimm.csproj
```

## Licencia

Uso personal/educativo. Los materiales descargados pertenecen a sus respectivos titulares; consulta las políticas de Vimm's Lair.
