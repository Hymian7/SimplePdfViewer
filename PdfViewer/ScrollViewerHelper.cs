using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PdfViewer
{

    //https://stackoverflow.com/questions/3727439/how-to-enable-horizontal-scrolling-with-mouse
    public static class ScrollViewerHelper
    {

        public static T FindDescendant<T>(this DependencyObject d) where T : DependencyObject
        {
            if (d == null)
                return null;

            var childCount = VisualTreeHelper.GetChildrenCount(d);

            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);

                var result = child as T ?? FindDescendant<T>(child);

                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
