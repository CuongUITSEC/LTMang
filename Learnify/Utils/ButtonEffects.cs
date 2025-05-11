using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Learnify.Utils
{
    public static class ButtonEffects
    {
        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "HoverBackground",
                typeof(Brush),
                typeof(ButtonEffects),
                new PropertyMetadata(Brushes.LightGray));

        public static Brush GetHoverBackground(DependencyObject obj) =>
            (Brush)obj.GetValue(HoverBackgroundProperty);

        public static void SetHoverBackground(DependencyObject obj, Brush value) =>
            obj.SetValue(HoverBackgroundProperty, value);

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "PressedBackground",
                typeof(Brush),
                typeof(ButtonEffects),
                new PropertyMetadata(Brushes.DarkGray));

        public static Brush GetPressedBackground(DependencyObject obj) =>
            (Brush)obj.GetValue(PressedBackgroundProperty);

        public static void SetPressedBackground(DependencyObject obj, Brush value) =>
            obj.SetValue(PressedBackgroundProperty, value);
    }
}