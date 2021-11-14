# SimplePdfViewer
A Simple Pdf Viewer for WPF projects using the Windows 10 API

Check out https://blogs.u2u.be/lander/post/2018/01/23/Creating-a-PDF-Viewer-in-WPF-using-Windows-10-APIs for the idea behind.
I added support for Zooming and Panning as well as navigation buttons and the possibility to jump directly to an indicated page.

Nuget Package available under
https://www.nuget.org/packages/SimplePdfViewer.PdfViewer/


# Screenshot
![Screenshot of PdfViewer](https://github.com/Hymian7/SimplePdfViewer/blob/cbcd716def52924079ce3c2bfd13e277e7050e38/Screenshot%201.png)

# Usage
1. Reference the project or the [NuGet package](https://www.nuget.org/packages/SimplePdfViewer.PdfViewer/)
2. In your XAML File add the control using the following snippet:
```
<pdfviewer:PdfViewer Name="PdfViewerControl" Height="Auto"></pdfviewer:PdfViewer>
```
