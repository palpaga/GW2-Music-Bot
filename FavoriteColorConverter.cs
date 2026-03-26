using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Linq;

namespace Gw2MusicBot
{
    public class FavoriteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string checksum)
            {
                var favs = ConfigManager.Config.Favorites;
                if (favs != null && favs.Any(f => f.Checksum == checksum))
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")); // Yellow
                }
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
