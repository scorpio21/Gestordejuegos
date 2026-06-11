using Avalonia.Controls;

namespace GestorJuegos.Views.Windows
{
    public partial class MainWindow : Window
    {
        private void ShowHelpExternalLib()
        {
            string helpText = "📚 IMPORTAR BIBLIOTECA EXTERNA\n\n" +
                "Esta función permite integrar una colección ya existente de otros gestores de juegos (como Biblioteca Externa, Big Box u otros sistemas basados en XML).\n\n" +
                "¿CÓMO FUNCIONA?:\n" +
                "1. Selecciona la carpeta raíz de tu biblioteca externa en la configuración.\n" +
                "2. El sistema detectará automáticamente los metadatos y las rutas de las imágenes en las carpetas estándar (Data, Images).\n" +
                "3. Se importarán juegos, plataformas, categorías y valoraciones de la comunidad.\n\n" +
                "VENTAJAS:\n" +
                "- Reutiliza todo tu arte multimedia sin duplicar archivos.\n" +
                "- Mantiene la compatibilidad con tus rutas de juegos actuales.\n" +
                "- Permite una transición fluida hacia este gestor sin perder tu progreso.";

            ShowMessage(helpText);
        }

        private void ShowHelpImportFolder()
        {
            string helpText = "📁 IMPORTAR DESDE CARPETA (ESCÁNER LOCAL)\n\n" +
                "Ideal para añadir juegos nuevos o colecciones que tienes organizadas simplemente en carpetas locales.\n\n" +
                "PASOS A SEGUIR:\n" +
                "1. Selecciona la carpeta que contiene las ROMs o ejecutables de una consola.\n" +
                "2. El sistema creará entradas individuales por cada archivo compatible detectado.\n" +
                "3. Tras la importación, el sistema buscará automáticamente carátulas y descripciones en tu biblioteca local.\n\n" +
                "CONSEJO DE ORO:\n" +
                "Para un reconocimiento del 100%, intenta que el nombre del archivo coincida con el título oficial del juego.";

            ShowMessage(helpText);
        }

        private void ShowHelpEmulator()
        {
            string helpText = "🎮 CONFIGURACIÓN DE EMULADORES\n\n" +
                "Para que los juegos funcionen, debes asignar un ejecutable a cada plataforma:\n\n" +
                "1. Ve a 'Gestionar Plataformas' (icono ⚙️).\n" +
                "2. Selecciona la consola deseada.\n" +
                "3. En 'Ruta del Emulador', busca el archivo .exe de tu emulador.\n" +
                "4. Añade argumentos de línea de comandos si son necesarios (ej: -f -L cores\\snes9x_libretro.dll).\n\n" +
                "NOTA: Puedes cambiar el emulador para un juego individual editando su ficha y yendo a la pestaña 'Lanzamiento'.";

            ShowMessage(helpText);
        }

        private void ShowHelpMultiDisk()
        {
            string helpText = "💿 SOPORTE PARA JUEGOS MULTI-DISCO\n\n" +
                "Si un juego tiene varios discos (CD1, CD2, etc.), puedes agruparlos en una sola ficha:\n\n" +
                "1. Edita el juego y ve a la pestaña 'Archivos'.\n" +
                "2. Pulsa el botón '+' para añadir Disco 2, Disco 3, etc.\n\n" +
                "3. CÓMO JUGAR:\n" +
                "   Al pulsar el botón 'JUGAR', si hay varios discos, aparecerá un selector desplegable (flecha ▼) para elegir qué disco iniciar.";

            ShowMessage(helpText);
        }

        private void ShowHelpDatabase()
        {
            string helpText = "🗄️ BASES DE DATOS Y RESPALDOS\n\n" +
                "• ARQUITECTURA:\n" +
                "  Tus datos se separan para mayor velocidad:\n" +
                "  - GestorJuegos.db: Información de textos.\n" +
                "  - GestorCovers.db: Imágenes y miniaturas.\n\n" +
                "• CONSEJOS:\n" +
                "  - 'Sincronizar Rutas': Úsalo si mueves tu biblioteca externa a otra unidad de disco.\n" +
                "  - 'Base Maestra': Consulta nuestra DB de miles de juegos para rellenar información faltante automáticamente.\n\n" +
                "• RESPALDOS:\n" +
                "  Usa la opción de exportar periódicamente para no perder tus avances y favoritos.";

            ShowMessage(helpText);
        }

        private void ShowAbout()
        {
            string aboutText = "🎮 GESTOR DE JUEGOS v1.0.9.5\n\n" +
                "Un organizador integral para colecciones de juegos retro, optimizado para grandes bibliotecas y uso con mando.\n\n" +
                "👨‍💻 Autor: Scorpio\n" +
                "📂 Repositorio: https://github.com/scorpio21/Gestordejuegos\n\n" +
                "🔥 NOVEDADES v1.0.9.5:\n" +
                "• Arquitectura de Base de Datos Dual (Datos + Multimedia).\n" +
                "• Sistema de Miniaturas con SkiaSharp.\n" +
                "• Drag & Drop recursivo de carpetas.\n" +
                "• Estadísticas visuales en el Dashboard.\n" +
                "• Filtros temporales y ordenación avanzada.";

            ShowMessage(aboutText);
        }
    }
}
