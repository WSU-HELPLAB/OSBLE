using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

/// <summary>
/// This is how you get the descendents inside of an element
/// </summary>
public static class VisualTreeEnumeration
{
    public static IEnumerable<DependencyObject> Descendents(this DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendent in Descendents(child))
                yield return descendent;
        }
    }
}