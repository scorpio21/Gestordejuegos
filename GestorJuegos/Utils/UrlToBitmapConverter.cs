using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GestorJuegos.Utils
{
    public class UrlToBitmapConverter : IValueConverter
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                // Para Avalonia, los conversores deben ser síncronos. 
                // Cargaremos la imagen de forma asíncrona y devolveremos un placeholder o nulo mientras tanto.
                // Sin embargo, para simplificar, usaremos un truco o simplemente devolveremos null si no está en caché.
                // Lo ideal es que el ViewModel maneje la descarga.
                
                return Task.Run(async () => await LoadBitmap(url)).Result;
            }
            return null;
        }

        private async Task<Bitmap?> LoadBitmap(string url)
        {
            try
            {
                var data = await _httpClient.GetByteArrayAsync(url);
                return new Bitmap(new MemoryStream(data));
            }
            catch { return null; }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
