using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MapSearchApp
{
    public partial class ImageDisplayWindow : Window
    {
        private BitmapSource _fullMap;
        private Int32Rect _baseCropRect;
        private double _zoomMultiplier = 2.0; // Starts at 2.0x radius like original Math
        private Point? _dragStartPoint;
        private double _panOffsetX = 0;
        private double _panOffsetY = 0;

        public ImageDisplayWindow(BitmapSource contextImg, BitmapSource fullMap, Int32Rect baseCrop, string ocrText, Rect highlight)
        {
            InitializeComponent();
            _fullMap = fullMap;
            _baseCropRect = baseCrop;

            ContextImageViewer.Source = contextImg;
            OcrResultText.Text = string.IsNullOrWhiteSpace(ocrText) ? "OCR Did not find any text on the cropped map region." : $"Detected Map Number: {ocrText}";

            TargetHighlight.Width = highlight.Width;
            TargetHighlight.Height = highlight.Height;
            Canvas.SetLeft(TargetHighlight, highlight.X);
            Canvas.SetTop(TargetHighlight, highlight.Y);

            RegenerateCrop();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            // Zoom In = Smaller crop box = fewer pixels = looks closer (if stretched) or smaller image natively
            if (_zoomMultiplier > 0.5) 
            {
                _zoomMultiplier -= 0.5;
                RegenerateCrop();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            // Zoom Out = Larger crop box = more pixels = shows more surrounding area
            _zoomMultiplier += 0.5;
            RegenerateCrop();
        }

        private void TargetPanel_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            if (sender is UIElement el) el.CaptureMouse();
        }

        private void TargetPanel_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_dragStartPoint.HasValue)
            {
                Point currentPoint = e.GetPosition(this);
                Vector delta = _dragStartPoint.Value - currentPoint;
                
                // Adjust pan offsets depending on zoom multiplier and rough scaling bounds
                double scaleRatio = ((double)_fullMap.PixelWidth / CroppedImageViewer.ActualWidth) * (_zoomMultiplier / 2.0);
                
                _panOffsetX += delta.X * scaleRatio;
                _panOffsetY += delta.Y * scaleRatio;

                _dragStartPoint = currentPoint;
                RegenerateCrop();
            }
        }

        private void TargetPanel_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_dragStartPoint.HasValue)
            {
                if (sender is UIElement el) el.ReleaseMouseCapture();
                _dragStartPoint = null;
            }
        }

        private void RegenerateCrop()
        {
            int expandedH = (int)(_baseCropRect.Height * _zoomMultiplier);
            int expandedW = (int)(expandedH * 1.5);
            
            int expandedX = System.Math.Max(0, _baseCropRect.X + (_baseCropRect.Width / 2) - (expandedW / 2) + (int)_panOffsetX);
            int expandedY = System.Math.Max(0, _baseCropRect.Y + (_baseCropRect.Height / 2) - (expandedH / 2) + (int)_panOffsetY);

            // Clamp max bounds
            expandedX = System.Math.Min(expandedX, (int)_fullMap.PixelWidth - expandedW);
            expandedY = System.Math.Min(expandedY, (int)_fullMap.PixelHeight - expandedH);
            
            // Re-clamp if negative (to prevent crashes on very large zooms near edges)
            expandedX = System.Math.Max(0, expandedX);
            expandedY = System.Math.Max(0, expandedY);
            
            expandedW = System.Math.Min((int)_fullMap.PixelWidth - expandedX, expandedW);
            expandedH = System.Math.Min((int)_fullMap.PixelHeight - expandedY, expandedH);

            if (expandedW > 0 && expandedH > 0)
            {
                Int32Rect cropRect = new Int32Rect(expandedX, expandedY, expandedW, expandedH);
                CroppedBitmap targetCropped = new CroppedBitmap(_fullMap, cropRect);
                CroppedImageViewer.Source = targetCropped;
            }
        }
        private void TifFilePathBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OpenTifFile(TifFilePathBox.Text.Trim());
            }
        }

        private void BrowseTif_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "TIF Images|*.tif;*.tiff|All Files|*.*";
            if (dlg.ShowDialog() == true)
            {
                TifFilePathBox.Text = dlg.FileName;
                OpenTifFile(dlg.FileName);
            }
        }

        private void OpenTifFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim();
            
            // If the user pasted an absolute path that exists, just open it
            if (System.IO.File.Exists(path))
            {
                LaunchTif(path);
                return;
            }

            // Standardize to just the filename to append .tif cleanly
            string nameOnly = path;
            if (nameOnly.ToLower().EndsWith(".tif"))
                nameOnly = nameOnly.Substring(0, nameOnly.Length - 4);
            else if (nameOnly.ToLower().EndsWith(".tiff"))
                nameOnly = nameOnly.Substring(0, nameOnly.Length - 5);

            string filenameWithExt = nameOnly + ".tif";

            // Check Custom Directory if settings file exists
            try
            {
                string configPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "tif_directory.txt");
                if (System.IO.File.Exists(configPath))
                {
                    string customTifDir = System.IO.File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrEmpty(customTifDir) && System.IO.Directory.Exists(customTifDir))
                    {
                        string customPath = System.IO.Path.Combine(customTifDir, filenameWithExt);
                        if (System.IO.File.Exists(customPath))
                        {
                            LaunchTif(customPath);
                            return;
                        }
                    }
                }
            }
            catch { /* Ignore config read errors, just fall through */ }

            // Check working directory
            if (System.IO.File.Exists(filenameWithExt))
            {
                LaunchTif(filenameWithExt);
                return;
            }

            // Check Base Directory
            string inBaseDir = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, filenameWithExt);
            if (System.IO.File.Exists(inBaseDir))
            {
                LaunchTif(inBaseDir);
                return;
            }

            // Traverse upwards to aggressively find any "TIFs" folder
            string? currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                string searchDir = System.IO.Path.Combine(currentDir, "TIFs", filenameWithExt);
                if (System.IO.File.Exists(searchDir))
                {
                    LaunchTif(searchDir);
                    return;
                }
                
                // Also check a pure directory match (e.g. if they put it one level up)
                string pureDir = System.IO.Path.Combine(currentDir, filenameWithExt);
                if (System.IO.File.Exists(pureDir))
                {
                    LaunchTif(pureDir);
                    return;
                }

                currentDir = System.IO.Path.GetDirectoryName(currentDir);
            }

            MessageBox.Show("File not found: " + path);
        }

        private void LaunchTif(string path)
        {
            try
            {
                var pInfo = new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true };
                System.Diagnostics.Process.Start(pInfo);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Could not open TIF file: " + ex.Message);
            }
        }
    }
}
