using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PdfViewer
{
    /// <summary>
    /// Interaktionslogik für PdfViewer.xaml
    /// </summary>
    public partial class PdfViewer : UserControl
    {
        #region Bindable Properties

        public string PdfPath
        {
            get { return (string)GetValue(PdfPathProperty); }
            set { SetValue(PdfPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PdfPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PdfPathProperty =
            DependencyProperty.Register("PdfPath", typeof(string), typeof(PdfViewer), new PropertyMetadata(null, propertyChangedCallback: OnPdfPathChanged));

        private static void OnPdfPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pdfDrawer = (PdfViewer)d;
            

            if (!string.IsNullOrEmpty(pdfDrawer.PdfPath))
            {
                //making sure it's an absolute path
                var path = System.IO.Path.GetFullPath(pdfDrawer.PdfPath);

                StorageFile.GetFileFromPathAsync(path).AsTask()
                  //load pdf document on background thread
                  .ContinueWith(t => PdfDocument.LoadFromFileAsync(t.Result).AsTask()).Unwrap()
                  //display on UI Thread
                  .ContinueWith(t2 => PdfToImages(pdfDrawer, t2.Result), TaskScheduler.FromCurrentSynchronizationContext());             

            }

        }

        #endregion

        #region PrivateProperties
        private int PageCount => PagesContainer.Items.Count;
        
        private double _zoomFactor;

        public double ZoomFactor
        {
            get { return _zoomFactor; }
            set { _zoomFactor = value; }
        }

        //variables to store the offset values
        private double relX;
        private double relY;

        private bool mouseCaptured = false;

        private Point scrollMousePoint = new Point();
        private double hOff = 1;
        private double vOff = 1;



        private void SetCurrentPage(int pageNumber)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(pageNumber)} must be greater than zero.");
            }

            if (pageNumber > PageCount)
            {
                throw new ArgumentOutOfRangeException($"{nameof(pageNumber)} can't be greater than the page count");
            }

            PagesContainer.ScrollIntoView(PagesContainer.Items[pageNumber - 1]); 
        }

    #endregion


    public PdfViewer()
        {
            InitializeComponent();

            PagesContainer.PreviewMouseWheel += PagesContainer_PreviewMouseWheel;
            ZoomFactor = 1.0;
            
        }

        private static async Task PdfToImages(PdfViewer pdfViewer, PdfDocument pdfDoc)
        {
            var items = pdfViewer.PagesContainer.Items;
            items.Clear();

            if (pdfDoc == null) return;

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using (var page = pdfDoc.GetPage(i))
                {
                    var bitmap = await PageToBitmapAsync(page);
                    var image = new Image
                    {
                        Source = bitmap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        //Margin = new Thickness(0, 4, 0, 4),                        
                        MaxWidth = 800,
                        ClipToBounds = true                        
                    };

                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);

                    items.Add(image);
                }
            }

            pdfViewer.tbPageCount.Text = pdfViewer.PagesContainer.Items.Count.ToString();
            pdfViewer.tbCurrentPage.Text = "1";
        }

        private static async Task<BitmapImage> PageToBitmapAsync(PdfPage page)
        {
            BitmapImage image = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                await page.RenderToStreamAsync(stream);

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream.AsStream();
                image.EndInit();
            }

            return image;
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            var curPage = GetCurrentPage();

            // Do nothing if already at the end
            if (curPage == PageCount) return;

            PagesContainer.ScrollIntoView(PagesContainer.Items[curPage]);
            
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            var curPage = GetCurrentPage();

            // Do nothing if on first page
            if (curPage == 1) return;

            PagesContainer.ScrollIntoView(PagesContainer.Items[curPage - 2]);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scroll = sender as ScrollViewer;
            //see if the content size is changed
            if (e.ExtentWidthChange != 0 || e.ExtentHeightChange != 0)
            {
                //calculate and set accordingly
                scroll.ScrollToHorizontalOffset(CalculateOffset(e.ExtentWidth, e.ViewportWidth, scroll.ScrollableWidth, relX));
                scroll.ScrollToVerticalOffset(CalculateOffset(e.ExtentHeight, e.ViewportHeight, scroll.ScrollableHeight, relY));
            }
            else
            {
                //store the relative values if normal scroll
                relX = (e.HorizontalOffset + 0.5 * e.ViewportWidth) / e.ExtentWidth;
                relY = (e.VerticalOffset + 0.5 * e.ViewportHeight) / e.ExtentHeight;
            }

            tbCurrentPage.Text = GetCurrentPage().ToString();
            //Debug.Print($"Current Position: {ScrollViewer.VerticalOffset} -- MaxHeight: {ScrollViewer.ScrollableHeight}");
        }

        private static double CalculateOffset(double extent, double viewPort, double scrollWidth, double relBefore)
        {
            //calculate the new offset
            double offset = relBefore * extent - 0.5 * viewPort;
            //see if it is negative because of initial values
            if (offset < 0)
            {
                //center the content
                //this can be set to 0 if center by default is not needed
                offset = 0 * scrollWidth;
            }
            return offset;
        }

        private int GetCurrentPage()
        {
            var curPos = ScrollViewer.VerticalOffset <= 0 ? 0.01 : ScrollViewer.VerticalOffset;
            var totalHeight = ScrollViewer.ScrollableHeight;

            return (int)Math.Ceiling((curPos / totalHeight * PageCount));
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Check for Horizontal Scrolling
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {

                if (e.Delta < 0)
                    ScrollViewer.LineRight();
                else
                    ScrollViewer.LineLeft();

                e.Handled = true;
            }

            //Check for Zooming
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {

                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/0e2e4ce3-3310-478a-b204-bc8b5b543d58/how-to-zoomin-wpf-listview-content-without-zooming-scrollviewer?forum=wpf

                var _element = PagesContainer;
                var delta = e.Delta;
                var zoomScale = delta / 750.0;
                //_element.RenderTransformOrigin = e.GetPosition(PagesContainer);
                

                if (ZoomFactor+zoomScale > 0.1)
                {
            
                    var newZoomFactor = ZoomFactor += zoomScale;
                    _element.LayoutTransform = new ScaleTransform(newZoomFactor, newZoomFactor);
                    ZoomFactor = newZoomFactor;
                }

                e.Handled = true;

            }
            

        }

        private void PagesContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void tbCurrentPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int newPage;
                if(Int32.TryParse(tbCurrentPage.Text, out newPage))
                {
                    if(newPage >= 1 && newPage <= PageCount)
                    {
                        SetCurrentPage(newPage);
                    }
                }

                tbCurrentPage.Text = GetCurrentPage().ToString();

            }
        }

        private void tbCurrentPage_MouseLeave(object sender, MouseEventArgs e)
        {
            tbCurrentPage.Text = GetCurrentPage().ToString();
        }

        private void PagesContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scrollMousePoint = e.GetPosition(ScrollViewer);
            hOff = ScrollViewer.HorizontalOffset;
            vOff = ScrollViewer.VerticalOffset;
            ScrollViewer.CaptureMouse();

            mouseCaptured = true;
            Cursor = Cursors.ScrollAll;
        }

        private void PagesContainer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mouseCaptured)
            {
                ScrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(ScrollViewer).X));
                ScrollViewer.ScrollToVerticalOffset(vOff + (scrollMousePoint.Y - e.GetPosition(ScrollViewer).Y));
            }
        }

        private void ScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseCaptured = false;
            ScrollViewer.ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }
    }
}
