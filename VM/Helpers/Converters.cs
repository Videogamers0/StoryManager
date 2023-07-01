using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using System.Windows.Markup;

namespace StoryManager.VM.Helpers
{
    public class MultiLogicalAndBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = true;
            foreach (object value in values)
            {
                if (value is bool)
                    result = result && (bool)value;
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiLogicalOrBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            foreach (object value in values)
            {
                if (value is bool)
                    result = result || (bool)value;
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiLogicalAndBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = true;
            foreach (object value in values)
            {
                if (value is bool)
                    visible = visible && (bool)value;
                else if (value is Visibility vis)
                    visible = visible && vis == Visibility.Visible;
            }

            if (visible)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiLogicalOrBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = false;
            foreach (object value in values)
            {
                if (value is bool)
                    visible = visible || (bool)value;
                else if (value is Visibility vis)
                    visible = visible || vis == Visibility.Visible;
            }

            if (visible)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }

    public sealed class InverseBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public InverseBooleanToVisibilityConverter() :
            base(Visibility.Collapsed, Visibility.Visible)
        { }
    }

    public sealed class InverseBooleanToVisibilityHiddenConverter : BooleanConverter<Visibility>
    {
        public InverseBooleanToVisibilityHiddenConverter() :
            base(Visibility.Hidden, Visibility.Visible)
        { }
    }

    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        { }
    }

    public sealed class BooleanToVisibilityHiddenConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityHiddenConverter() :
            base(Visibility.Visible, Visibility.Hidden)
        { }
    }

    public class InverseVisibilityConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public sealed class InverseBooleanConverter : BooleanConverter<bool>
    {
        public InverseBooleanConverter() :
            base(false, true)
        { }
    }

    public class LiteroticaStoryUriConverter : IValueConverter
    {
        private const string BaseAddress = @"https://www.literotica.com/s/";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new Uri($"{BaseAddress}{value}");
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LiteroticaCategoryUriConverter : IValueConverter
    {
        private const string BaseAddress = @"https://www.literotica.com/c/";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new Uri($"{BaseAddress}{value}");
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LiteroticaAuthorUriConverter : IValueConverter
    {
        private const string BaseAddress = @"https://www.literotica.com/stories/memberpage.php?uid=";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new Uri($"{BaseAddress}{value}&page=submissions");
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
