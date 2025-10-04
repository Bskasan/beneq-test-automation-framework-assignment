using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Control.Wpf;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Running" => new SolidColorBrush(Colors.Green),
                "Idle" => new SolidColorBrush(Colors.Gray),
                "Error" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            };
        }
        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
