using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapSearchApp
{
    public partial class MainWindow : Window
    {
        private const int TotalColumns = 42; // R23E to R29E (7 ranges) x 6 sections
        private const int TotalRows = 36;    // T2N to T4S (6 townships) x 6 sections
        private BitmapSource? _hiResMapSource;
        private Point? _dragStartPoint;

        // Update manifest: host a JSON file at this URL with {"version":"1.0.3","url":"https://..."}
        private const string UpdateManifestUrl = "https://raw.githubusercontent.com/band72/TaxPanelApp/main/version.json";
        private const string CurrentVersion    = "1.0.3";
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MapScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(MapScrollViewer);
            MapScrollViewer.Cursor = Cursors.SizeAll;
            MapScrollViewer.CaptureMouse();
        }

        private void MapScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStartPoint.HasValue)
            {
                Point currentPoint = e.GetPosition(MapScrollViewer);
                Vector delta = _dragStartPoint.Value - currentPoint;
                
                // Reduce sensitivity (user requested "too sensitive")
                MapScrollViewer.ScrollToHorizontalOffset(MapScrollViewer.HorizontalOffset + (delta.X * 0.4));
                MapScrollViewer.ScrollToVerticalOffset(MapScrollViewer.VerticalOffset + (delta.Y * 0.4));
                
                _dragStartPoint = currentPoint;
            }
        }

        private void MapScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragStartPoint.HasValue)
            {
                MapScrollViewer.Cursor = Cursors.Arrow;
                MapScrollViewer.ReleaseMouseCapture();
                _dragStartPoint = null;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveOffsets();
        }

        private void LoadOffsets()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offsets.txt");
                if (System.IO.File.Exists(configPath))
                {
                    string[] parts = System.IO.File.ReadAllText(configPath).Split(',');
                    if (parts.Length == 4)
                    {
                        if (double.TryParse(parts[0], out double l)) LeftSlider.Value = l;
                        if (double.TryParse(parts[1], out double t)) TopSlider.Value = t;
                        if (double.TryParse(parts[2], out double r)) RightSlider.Value = r;
                        if (double.TryParse(parts[3], out double b)) BottomSlider.Value = b;
                    }
                }
            }
            catch { }
        }

        private void SaveOffsets()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offsets.txt");
                string content = $"{LeftSlider.Value},{TopSlider.Value},{RightSlider.Value},{BottomSlider.Value}";
                System.IO.File.WriteAllText(configPath, content);
            }
            catch { }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOffsets();
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.png");
                if (System.IO.File.Exists(path))
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    MapImage.Source = bmp;
                }

                // Autoload from Tax-Maps folder
                string taxMapsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tax-Maps");
                string defaultPdf = System.IO.Path.Combine(taxMapsPath, "0 Tile Map Index.pdf");
                if (!System.IO.File.Exists(defaultPdf))
                {
                    // Fallback for debug environment
                    string debugTaxMapsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Tax-Maps");
                    defaultPdf = System.IO.Path.Combine(debugTaxMapsPath, "0 Tile Map Index.pdf");
                }

                if (System.IO.File.Exists(defaultPdf))
                {
                    try
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(System.IO.Path.GetFullPath(defaultPdf));
                        var pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                        var page = pdfDoc.GetPage(0);

                        uint renderWidth = 4000;
                        double ratio = (double)renderWidth / page.Size.Width;
                        var renderOptions = new Windows.Data.Pdf.PdfPageRenderOptions
                        {
                            DestinationWidth = renderWidth,
                            DestinationHeight = (uint)(page.Size.Height * ratio)
                        };

                        using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                        {
                            await page.RenderToStreamAsync(stream, renderOptions);
                            await stream.FlushAsync();
                            stream.Seek(0);
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = stream.AsStream();
                            bitmapImage.EndInit();
                            
                            _hiResMapSource = bitmapImage;
                            ResultLabel.Text = $"Loaded Tax-Maps PDF: {_hiResMapSource.PixelWidth}x{_hiResMapSource.PixelHeight}";
                        }
                    }
                catch (Exception pdfEx)
                {
                    ErrorReporter.Report(pdfEx, "AutoLoad PDF");
                    MessageBox.Show("Could not load default PDF: " + pdfEx.Message);
                }
                }

                // Fallback to hires_map.png
                if (_hiResMapSource == null)
                {
                    string hiresPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hires_map.png");
                    if (System.IO.File.Exists(hiresPath))
                    {
                        BitmapImage hiresBmp = new BitmapImage();
                        hiresBmp.BeginInit();
                        hiresBmp.UriSource = new Uri(hiresPath, UriKind.Absolute);
                        hiresBmp.CacheOption = BitmapCacheOption.OnLoad;
                        hiresBmp.EndInit();
                        _hiResMapSource = hiresBmp;
                        ResultLabel.Text = $"Autoloaded High-Res Map: {_hiResMapSource.PixelWidth}x{_hiResMapSource.PixelHeight}";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex, "MainWindow_Loaded");
                MessageBox.Show("Initialization error (could not load map images): " + ex.Message, "Startup Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            UpdateGridOverlay();
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAlgorithm();
        }

        private async void BrowseMap_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Map Files|*.png;*.jpg;*.jpeg;*.pdf|All Files|*.*";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (dlg.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(dlg.FileName);
                        var pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                        var page = pdfDoc.GetPage(0);

                        // Render at very high resolution (e.g., 4000 pixels wide) to provide immense optical zoom
                        uint renderWidth = 4000;
                        double ratio = (double)renderWidth / page.Size.Width;
                        var renderOptions = new Windows.Data.Pdf.PdfPageRenderOptions
                        {
                            DestinationWidth = renderWidth,
                            DestinationHeight = (uint)(page.Size.Height * ratio)
                        };

                        using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                        {
                            await page.RenderToStreamAsync(stream, renderOptions);
                            await stream.FlushAsync();
                            stream.Seek(0);

                            string hiresPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hires_map.png");
                            try
                            {
                                using (var fileStream = System.IO.File.Create(hiresPath))
                                {
                                    stream.AsStream().CopyTo(fileStream);
                                }
                            }
                            catch (Exception ioEx)
                            {
                                MessageBox.Show("Could not save PDF dynamically to disk. Image will still load into memory. Error: " + ioEx.Message, "File Write Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            stream.Seek(0);

                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = stream.AsStream();
                            bitmapImage.EndInit();
                            _hiResMapSource = bitmapImage;
                        }
                    }
                    else
                    {
                        string hiresPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hires_map.png");
                        try
                        {
                            System.IO.File.Copy(dlg.FileName, hiresPath, true);
                        }
                        catch (Exception ioEx)
                        {
                            MessageBox.Show("Could not copy the selected image file locally. It may be locked by another process. Will load directly from source. Error: " + ioEx.Message, "File Copy Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hiresPath = dlg.FileName; // Fallback to loading the original directly map if copying fails
                        }

                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(hiresPath, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        _hiResMapSource = bmp;
                    }
                    
                    ResultLabel.Text = $"High-Res Map Loaded for Zooming! Res: {_hiResMapSource.PixelWidth}x{_hiResMapSource.PixelHeight}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Critical Error loading map: " + ex.Message, "Browse Map Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Directory Containing TIF Maps"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string path = dialog.FolderName;
                    string configPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "tif_directory.txt");
                    System.IO.File.WriteAllText(configPath, path);
                    MessageBox.Show($"TIF directory saved to:\n{path}", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdates.IsEnabled = false;
            BtnCheckUpdates.Content   = "Checking…";
            try
            {
                // Run the HTTP call off the UI thread so the window never freezes
                string json = await Task.Run(() => _httpClient.GetStringAsync(UpdateManifestUrl)).ConfigureAwait(true);
                using JsonDocument doc = JsonDocument.Parse(json);
                string remoteVersion = doc.RootElement.GetProperty("version").GetString() ?? string.Empty;
                string downloadUrl   = doc.RootElement.TryGetProperty("url", out var urlEl)
                                       ? (urlEl.GetString() ?? string.Empty) : string.Empty;

                if (string.IsNullOrEmpty(remoteVersion))
                {
                    MessageBox.Show("Could not read version from update manifest.", "Update Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.Compare(remoteVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    var result = MessageBox.Show(
                        $"A new version is available: v{remoteVersion}\nYou have: v{CurrentVersion}\n\nOpen the download page?",
                        "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(downloadUrl))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"You are up to date (v{CurrentVersion}).", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex, "CheckUpdates");
                MessageBox.Show("Update check failed: " + ex.Message, "Update Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                BtnCheckUpdates.IsEnabled = true;
                BtnCheckUpdates.Content   = "↑ Updates";
            }
        }
        private void PanelInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteAlgorithm();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            UpdateGridOverlay();
            UpdateHighlightPosition();
            SaveOffsets();
        }

        private void UpdateHighlightPosition()
        {
            if (string.IsNullOrWhiteSpace(PanelInput.Text) || PanelInput.Text.Length != 4) return;
            try
            {
                string panelStr = PanelInput.Text.Trim();
                int rDigit = int.Parse(panelStr.Substring(0, 1));
                int tDigit = int.Parse(panelStr.Substring(1, 1));
                int sVal = int.Parse(panelStr.Substring(2, 2));

                int rIndex = rDigit - 3; 
                int tIndex = tDigit - 1; 

                int sectionZeroBased = sVal - 1;
                int rowInTownship = sectionZeroBased / 6;
                int colInTownship = sectionZeroBased % 6;

                if (rowInTownship % 2 == 0) colInTownship = 5 - colInTownship;

                double gridCol = rIndex * 6 + colInTownship - 1.5;
                double gridRow = tIndex * 6 + rowInTownship - 3.0;

                double baseWidth = 1024.0;
                double baseHeight = 1024.0;
                double actualWidth = (MapImage.Source as BitmapSource)?.PixelWidth ?? baseWidth;
                double actualHeight = (MapImage.Source as BitmapSource)?.PixelHeight ?? baseHeight;
                double scaleX = actualWidth / baseWidth;
                double scaleY = actualHeight / baseHeight;

                double left = LeftSlider.Value * scaleX;
                double top = TopSlider.Value * scaleY;
                double right = RightSlider.Value * scaleX;
                double bottom = BottomSlider.Value * scaleY;
                
                double gridWidth = actualWidth - left - right;
                double gridHeight = actualHeight - top - bottom;

                double secW = gridWidth / TotalColumns;
                double secH = gridHeight / TotalRows;

                double targetX = left + gridCol * secW;
                double targetY = top + gridRow * secH;

                HighlightBox.Width = secW;
                HighlightBox.Height = secH;
                Canvas.SetLeft(HighlightBox, targetX);
                Canvas.SetTop(HighlightBox, targetY);
                HighlightBox.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void ShowGridCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateGridOverlay();
        }

        private void UpdateGridOverlay()
        {
            // Clear previous grid lines
            for (int i = OverlayCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (OverlayCanvas.Children[i] is Line)
                {
                    OverlayCanvas.Children.RemoveAt(i);
                }
            }

            if (ShowGridCheck.IsChecked != true) return;

            // Determine dynamic scaling based on loaded image, defaulting to 1024 base if null
            double baseWidth = 1024.0;
            double baseHeight = 1024.0;
            double actualWidth = (MapImage.Source as BitmapSource)?.PixelWidth ?? baseWidth;
            double actualHeight = (MapImage.Source as BitmapSource)?.PixelHeight ?? baseHeight;
            double scaleX = actualWidth / baseWidth;
            double scaleY = actualHeight / baseHeight;

            double left = LeftSlider.Value * scaleX;
            double top = TopSlider.Value * scaleY;
            double right = RightSlider.Value * scaleX;
            double bottom = BottomSlider.Value * scaleY;
            
            double gridWidth = actualWidth - left - right;
            double gridHeight = actualHeight - top - bottom;

            double secW = gridWidth / TotalColumns;
            double secH = gridHeight / TotalRows;

            SolidColorBrush townshipBrush = new SolidColorBrush(Color.FromArgb(120, 0, 0, 255));
            SolidColorBrush sectionBrush = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0));

            // Draw vertical grid lines
            for (int c = 0; c <= TotalColumns; c++)
            {
                double x = left + c * secW;
                Line line = new Line
                {
                    X1 = x, Y1 = top,
                    X2 = x, Y2 = top + gridHeight,
                    Stroke = (c % 6 == 0) ? townshipBrush : sectionBrush,
                    StrokeThickness = (c % 6 == 0) ? 2 : 1
                };
                OverlayCanvas.Children.Add(line);
            }

            // Draw horizontal lines
            for (int r = 0; r <= TotalRows; r++)
            {
                double y = top + r * secH;
                Line line = new Line
                {
                    X1 = left, Y1 = y,
                    X2 = left + gridWidth, Y2 = y,
                    Stroke = (r % 6 == 0) ? townshipBrush : sectionBrush,
                    StrokeThickness = (r % 6 == 0) ? 2 : 1
                };
                OverlayCanvas.Children.Add(line);
            }
        }

        private async void ExecuteAlgorithm()
        {
            string panelStr = PanelInput.Text.Trim();
            if (panelStr.Length != 4)
            {
                ResultLabel.Text = "Invalid Panel Number. Must be 4 digits. Example: 6307";
                HighlightBox.Visibility = Visibility.Hidden;
                return;
            }

            try
            {
                // Format: R T SS (e.g. 6 3 07)
                // R corresponds to Range (3=R23E, 4=R24E, 5=R25E, 6=R26E, 7=R27E, 8=R28E, 9=R29E)
                // T corresponds to Township (1=T2N, 2=T1N, 3=T1S, 4=T2S, 5=T3S, 6=T4S)
                // SS corresponds to Section (01-36)
                int rDigit = int.Parse(panelStr.Substring(0, 1));
                int tDigit = int.Parse(panelStr.Substring(1, 1));
                int sVal = int.Parse(panelStr.Substring(2, 2));

                if (rDigit < 3 || rDigit > 9)
                    throw new Exception("Range digit (first char) must be between 3 and 9 (R23E to R29E).");
                if (tDigit < 1 || tDigit > 6)
                    throw new Exception("Township digit (second char) must be between 1 and 6 (T2N to T4S).");
                if (sVal < 1 || sVal > 36)
                    throw new Exception("Section (last two chars) must be between 01 and 36.");

                // Map parsed data to 0-based indices
                // 3 -> R23E, ..., 9 -> R29E
                int rIndex = rDigit - 3; 
                // 1 -> T2N, ..., 6 -> T4S
                int tIndex = tDigit - 1; 

                // Determine Local Grid Col and Row for PLSS Section (1-36)
                int sectionZeroBased = sVal - 1;
                int rowInTownship = sectionZeroBased / 6;
                int colInTownship = sectionZeroBased % 6;

                // PLSS section numbering wraps back and forth horizontally
                if (rowInTownship % 2 == 0)
                {
                    // Even rows (0, 2, 4): snaking goes right-to-left
                    colInTownship = 5 - colInTownship;
                }

                // Adjust mathematical constants to calibrate highlight alignment to the new Map projection:
                // Left 1.5 panels (-1.5 column), Up 3.0 panels (-3.0 rows)
                double gridCol = rIndex * 6 + colInTownship - 1.5;
                double gridRow = tIndex * 6 + rowInTownship - 3.0;

                // Overlay over image
                double baseWidth = 1024.0;
                double baseHeight = 1024.0;
                double actualWidth = (MapImage.Source as BitmapSource)?.PixelWidth ?? baseWidth;
                double actualHeight = (MapImage.Source as BitmapSource)?.PixelHeight ?? baseHeight;
                double scaleX = actualWidth / baseWidth;
                double scaleY = actualHeight / baseHeight;

                double left = LeftSlider.Value * scaleX;
                double top = TopSlider.Value * scaleY;
                double right = RightSlider.Value * scaleX;
                double bottom = BottomSlider.Value * scaleY;
                
                double gridWidth = actualWidth - left - right;
                double gridHeight = actualHeight - top - bottom;

                double secW = gridWidth / TotalColumns;
                double secH = gridHeight / TotalRows;

                double targetX = left + gridCol * secW;
                double targetY = top + gridRow * secH;

                HighlightBox.Width = secW;
                HighlightBox.Height = secH;
                Canvas.SetLeft(HighlightBox, targetX);
                Canvas.SetTop(HighlightBox, targetY);
                HighlightBox.Visibility = Visibility.Visible;

                // Ensure the crop rect doesn't go outside the image bounds
                BitmapSource? sourceForCrop = _hiResMapSource ?? (MapImage.Source as BitmapSource);
                if (sourceForCrop == null)
                {
                    ResultLabel.Text = "Map image is not loaded correctly. Please browse for a map, or ensure a PDF is in the TaxMaps folder.";
                    HighlightBox.Visibility = Visibility.Hidden;
                    return;
                }

                // Calculate ratio between crop source and displayed map
                double cropRatioX = sourceForCrop.PixelWidth / actualWidth;
                double cropRatioY = sourceForCrop.PixelHeight / actualHeight;

                int cropX = Math.Max(0, (int)(targetX * cropRatioX));
                int cropY = Math.Max(0, (int)(targetY * cropRatioY));
                int cropW = Math.Min((int)sourceForCrop.PixelWidth - cropX, (int)(secW * cropRatioX));
                int cropH = Math.Min((int)sourceForCrop.PixelHeight - cropY, (int)(secH * cropRatioY));

                if (cropW > 0 && cropH > 0)
                {
                    // Expand zoom crop outwards by 2.0x to reduce the optical "zoom" by 50%
                    // The right panel is wide (aspect ratio ~1.5), so we calculate width based on height to fill the box natively.
                    int expandedH = (int)(cropH * 2.0);
                    int expandedW = (int)(expandedH * 1.5);
                    
                    int expandedX = Math.Max(0, cropX + (cropW / 2) - (expandedW / 2));
                    int expandedY = Math.Max(0, cropY + (cropH / 2) - (expandedH / 2));
                    expandedW = Math.Min((int)sourceForCrop.PixelWidth - expandedX, expandedW);
                    expandedH = Math.Min((int)sourceForCrop.PixelHeight - expandedY, expandedH);

                    Int32Rect cropRect = new Int32Rect(expandedX, expandedY, expandedW, expandedH);
                    CroppedBitmap targetCropped = new CroppedBitmap(sourceForCrop, cropRect);

                    // Create Context Crop (dynamically scaled to ~5x5 panels centered on target)
                    int contextW = (int)(secW * cropRatioX * 5);
                    int contextH = (int)(secH * cropRatioY * 5);
                    int contextX = Math.Max(0, cropX + (cropW / 2) - (contextW / 2));
                    int contextY = Math.Max(0, cropY + (cropH / 2) - (contextH / 2));

                    // Clamp to image bounds
                    contextW = Math.Min((int)sourceForCrop.PixelWidth - contextX, contextW);
                    contextH = Math.Min((int)sourceForCrop.PixelHeight - contextY, contextH);

                    Int32Rect contextRect = new Int32Rect(contextX, contextY, contextW, contextH);
                    CroppedBitmap contextCropped = new CroppedBitmap(sourceForCrop, contextRect);

                    // Determine highlight position relative to context crop
                    Rect highlightRect = new Rect(cropX - contextX, cropY - contextY, cropW, cropH);

                    // Upscale the target crop by 1x (reduced per user request)
                    ScaleTransform scale = new ScaleTransform(1.0, 1.0);
                    TransformedBitmap hqCrop = new TransformedBitmap(targetCropped, scale);

                    string ocrText = string.Empty;
                    try
                    {
                        byte[] pngBytes;
                        using (var ms = new MemoryStream())
                        {
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(hqCrop));
                            encoder.Save(ms);
                            pngBytes = ms.ToArray();
                        }

                        using (var ras = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                        {
                            using (var writer = new Windows.Storage.Streams.DataWriter(ras.GetOutputStreamAt(0)))
                            {
                                writer.WriteBytes(pngBytes);
                                await writer.StoreAsync();
                            }
                            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
                            var swBitmap = await decoder.GetSoftwareBitmapAsync();
                            
                            var ocrEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
                            if (ocrEngine != null)
                            {
                                var ocrResult = await ocrEngine.RecognizeAsync(swBitmap);
                                ocrText = ocrResult.Text.Replace('\r', ' ').Replace('\n', ' ').Replace("\r\n", " ").Trim();
                            }
                            else
                            {
                                ocrText = "OCR Engine unsupported.";
                            }
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        ocrText = "OCR Exception: " + ocrEx.Message;
                    }

                    ResultLabel.Text = $"Parsed: T={tIndex+1}, R={rIndex+3}, Sec={sVal} => Map({gridCol}, {gridRow}) | OCR: {ocrText}";

                    // Pass the context map (showing relationship to original) and the required crop data for interactive zooms
                    ImageDisplayWindow window = new ImageDisplayWindow(contextCropped, sourceForCrop, new Int32Rect(cropX, cropY, cropW, cropH), ocrText, highlightRect);
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ResultLabel.Text = "Parse Error: " + ex.Message;
                HighlightBox.Visibility = Visibility.Hidden;
            }
        }
    }
}