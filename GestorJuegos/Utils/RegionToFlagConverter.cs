using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;

namespace GestorJuegos.Utils
{
    public class RegionToFlagConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string region)
            {
                string assetName = string.Empty;
                if (region.Contains("US") || region.Contains("USA")) assetName = "us.png";
                else if (region.Contains("EU") || region.Contains("Europe")) assetName = "eu.png";
                else if (region.Contains("JP") || region.Contains("Japan")) assetName = "jp.png";
                
                if (!string.IsNullOrEmpty(assetName))
                {
                    try
                    {
                        return new Bitmap(AssetLoader.Open(new Uri($"avares://GestorJuegos/img/Banderas/{assetName}")));
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
