using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RasterRotation.Core;

namespace RasterRotation.Wpf;

public partial class MainWindow : Window
{
    public static readonly RoutedCommand OpenCommand = new();
    public static readonly RoutedCommand SaveCommand = new();
    public static readonly RoutedCommand SaveAsCommand = new();
    public static readonly RoutedCommand HelpCommand = new();
    public static readonly RoutedCommand FlipHorizontalCommand = new();
    public static readonly RoutedCommand FlipVerticalCommand = new();
    public static readonly RoutedCommand RotateLeftCommand = new();
    public static readonly RoutedCommand RotateRightCommand = new();

    private readonly IImageFileService _fileService = new WicImageFileService();
    private readonly IImageTransformer _transformer = new WicImageTransformer();
    private readonly AppSettingsService _settingsService = new();
    private readonly AppSettings _settings;

    private ImageDocument? _document;
    private string? _currentPath;
    private bool _cropMode;
    private Point? _cropStart;

    public MainWindow(string? initialFile)
    {
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(OpenCommand, OpenExecuted));
        CommandBindings.Add(new CommandBinding(SaveCommand, SaveExecuted));
        CommandBindings.Add(new CommandBinding(SaveAsCommand, SaveAsExecuted));
        CommandBindings.Add(new CommandBinding(HelpCommand, HelpExecuted));
        CommandBindings.Add(new CommandBinding(FlipHorizontalCommand, FlipHorizontalExecuted));
        CommandBindings.Add(new CommandBinding(FlipVerticalCommand, FlipVerticalExecuted));
        CommandBindings.Add(new CommandBinding(RotateLeftCommand, RotateLeftExecuted));
        CommandBindings.Add(new CommandBinding(RotateRightCommand, RotateRightExecuted));

        _settings = _settingsService.Load();
        ApplySettingsToUi();

        if (!string.IsNullOrWhiteSpace(initialFile))
        {
            LoadImage(initialFile);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        PersistSettingsFromUi();
        _settingsService.Save(_settings);
        base.OnClosing(e);
    }

    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть изображение",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.jfif;*.tif;*.tiff;*.bmp;*.gif;*.heic;*.heif;*.webp|Все файлы|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadImage(dialog.FileName);
        }
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        SaveImage(_currentPath ?? _document.SourcePath);
    }

    private void SaveAsClick(object sender, RoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Сохранить изображение",
            FileName = Path.GetFileName(_currentPath ?? _document.SourcePath),
            Filter = "Исходный формат|*" + _document.OriginalMetadata.Format.Extension + "|Все файлы|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveImage(dialog.FileName);
        }
    }

    private void ExitClick(object sender, RoutedEventArgs e) => Close();

    private void RotateLeftClick(object sender, RoutedEventArgs e) => RotateRightAngle(-90);

    private void RotateRightClick(object sender, RoutedEventArgs e) => RotateRightAngle(90);

    private void Rotate180Click(object sender, RoutedEventArgs e) => RotateRightAngle(180);

    private void FlipHorizontalClick(object sender, RoutedEventArgs e) => ApplyTransform(bitmap => _transformer.FlipHorizontal(bitmap));

    private void FlipVerticalClick(object sender, RoutedEventArgs e) => ApplyTransform(bitmap => _transformer.FlipVertical(bitmap));

    private void CropModeClick(object sender, RoutedEventArgs e)
    {
        _cropMode = CropModeButton.IsChecked == true;
        CropRectangle.Visibility = Visibility.Collapsed;
        DetailsTextBlock.Text = _cropMode
            ? "Выделите прямоугольную область мышью на изображении."
            : "Режим обрезки выключен.";
    }

    private void GridMenuClick(object sender, RoutedEventArgs e)
    {
        _settings.ShowGrid = GridMenuItem.IsChecked;
        GridCheckBox.IsChecked = _settings.ShowGrid;
        ApplyGrid();
    }

    private void GridCheckClick(object sender, RoutedEventArgs e)
    {
        _settings.ShowGrid = GridCheckBox.IsChecked == true;
        GridMenuItem.IsChecked = _settings.ShowGrid;
        ApplyGrid();
    }

    private void HelpClick(object sender, RoutedEventArgs e) => ShowHelp();

    private void ApplyFreeRotationClick(object sender, RoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        if (!double.TryParse(AngleTextBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var angle))
        {
            MessageBox.Show(this, "Введите угол в градусах.", "Некорректный угол", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var background = GetRotationBackground();
        var rotated = _transformer.RotateFree(_document.Bitmap, angle, background);
        _document.ReplaceBitmap(rotated, angle);
        AngleSlider.Value = 0;
        AngleTextBox.Text = "0";
        UpdatePreview();
    }

    private void AngleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AngleTextBox is not null)
        {
            AngleTextBox.Text = e.NewValue.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private void ThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _settings.Theme = GetSelectedComboText(ThemeComboBox, "Light");
        ApplyTheme();
        _settingsService.Save(_settings);
    }

    private void BackgroundSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _settings.Background = GetSelectedComboText(BackgroundComboBox, "Neutral");
        ApplyViewportBackground();
        _settingsService.Save(_settings);
    }

    private void CropCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_cropMode || _document is null)
        {
            return;
        }

        _cropStart = e.GetPosition(CropCanvas);
        Canvas.SetLeft(CropRectangle, _cropStart.Value.X);
        Canvas.SetTop(CropRectangle, _cropStart.Value.Y);
        CropRectangle.Width = 0;
        CropRectangle.Height = 0;
        CropRectangle.Visibility = Visibility.Visible;
        CropCanvas.CaptureMouse();
    }

    private void CropCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_cropStart is null || !_cropMode || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(CropCanvas);
        var x = Math.Min(_cropStart.Value.X, current.X);
        var y = Math.Min(_cropStart.Value.Y, current.Y);
        var width = Math.Abs(current.X - _cropStart.Value.X);
        var height = Math.Abs(current.Y - _cropStart.Value.Y);

        Canvas.SetLeft(CropRectangle, x);
        Canvas.SetTop(CropRectangle, y);
        CropRectangle.Width = width;
        CropRectangle.Height = height;
    }

    private void CropCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_cropStart is null || !_cropMode || _document is null)
        {
            return;
        }

        CropCanvas.ReleaseMouseCapture();
        var selected = new Rect(
            Canvas.GetLeft(CropRectangle),
            Canvas.GetTop(CropRectangle),
            CropRectangle.Width,
            CropRectangle.Height);
        CropRectangle.Visibility = Visibility.Collapsed;
        _cropStart = null;

        var crop = ConvertUiRectToBitmapRegion(selected);
        if (crop is null || crop.Value.Width < 2 || crop.Value.Height < 2)
        {
            return;
        }

        var cropped = _transformer.Crop(_document.Bitmap, crop.Value);
        _document.ReplaceBitmap(cropped);
        UpdatePreview();
    }

    private void LoadImage(string filePath)
    {
        try
        {
            _document = _fileService.Load(filePath);
            _currentPath = filePath;
            _document.Changed += (_, _) => UpdateStatus();
            UpdatePreview();
            DetailsTextBlock.Text = "Изображение загружено. Доступны обрезка, отражение и поворот.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось открыть файл", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveImage(string outputPath)
    {
        if (_document is null)
        {
            return;
        }

        try
        {
            PersistSettingsFromUi();
            outputPath = EnsureOriginalExtension(outputPath, _document);
            _fileService.Save(_document, outputPath, new ImageSaveOptions { JpegQuality = _settings.JpegQuality });
            _currentPath = outputPath;
            _settingsService.Save(_settings);
            UpdateStatus();
            DetailsTextBlock.Text = "Файл сохранен в выбранном формате.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось сохранить файл", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string EnsureOriginalExtension(string outputPath, ImageDocument document)
    {
        var originalExtension = document.OriginalMetadata.Format.Extension;
        if (string.IsNullOrWhiteSpace(originalExtension))
        {
            return outputPath;
        }

        var currentExtension = Path.GetExtension(outputPath);
        return string.Equals(currentExtension, originalExtension, StringComparison.OrdinalIgnoreCase)
            ? outputPath
            : Path.ChangeExtension(outputPath, originalExtension);
    }

    private void RotateRightAngle(int degrees)
    {
        if (_document is null)
        {
            return;
        }

        var rotated = _transformer.RotateRightAngle(_document.Bitmap, degrees);
        _document.ReplaceBitmap(rotated, degrees);
        UpdatePreview();
    }

    private void ApplyTransform(Func<System.Windows.Media.Imaging.BitmapSource, System.Windows.Media.Imaging.BitmapSource> transform)
    {
        if (_document is null)
        {
            return;
        }

        _document.ReplaceBitmap(transform(_document.Bitmap));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        PreviewImage.Source = _document?.Bitmap;
        UpdateStatus();
        ApplyGrid();
    }

    private void UpdateStatus()
    {
        if (_document is null)
        {
            StatusFileText.Text = "Файл не открыт";
            StatusSizeText.Text = "Размер: -";
            StatusAngleText.Text = "Угол: 0";
            StatusFormatText.Text = "Формат: -";
            return;
        }

        var fileName = Path.GetFileName(_currentPath ?? _document.SourcePath);
        var metadata = _document.CurrentMetadata;
        StatusFileText.Text = fileName;
        StatusSizeText.Text = $"Размер: {metadata.Width} x {metadata.Height}";
        StatusAngleText.Text = $"Угол: {_document.CurrentAngle:0.##}";
        StatusFormatText.Text = $"Формат: {metadata.Format.DisplayName}";
        DetailsTextBlock.Text = $"DPI: {metadata.DpiX:0.#} x {metadata.DpiY:0.#}. Исходный формат: {metadata.Format.Extension}.";
    }

    private CropRegion? ConvertUiRectToBitmapRegion(Rect uiRect)
    {
        if (_document is null)
        {
            return null;
        }

        var imageRect = GetDisplayedImageRect();
        if (imageRect.Width <= 0 || imageRect.Height <= 0)
        {
            return null;
        }

        var clipped = Rect.Intersect(uiRect, imageRect);
        if (clipped.IsEmpty)
        {
            return null;
        }

        var scaleX = _document.Bitmap.PixelWidth / imageRect.Width;
        var scaleY = _document.Bitmap.PixelHeight / imageRect.Height;
        var x = (int)Math.Round((clipped.X - imageRect.X) * scaleX);
        var y = (int)Math.Round((clipped.Y - imageRect.Y) * scaleY);
        var width = (int)Math.Round(clipped.Width * scaleX);
        var height = (int)Math.Round(clipped.Height * scaleY);
        return new CropRegion(x, y, width, height);
    }

    private Rect GetDisplayedImageRect()
    {
        if (_document is null || CropCanvas.ActualWidth <= 0 || CropCanvas.ActualHeight <= 0)
        {
            return Rect.Empty;
        }

        var scale = Math.Min(
            CropCanvas.ActualWidth / _document.Bitmap.PixelWidth,
            CropCanvas.ActualHeight / _document.Bitmap.PixelHeight);
        var width = _document.Bitmap.PixelWidth * scale;
        var height = _document.Bitmap.PixelHeight * scale;
        var x = (CropCanvas.ActualWidth - width) / 2.0;
        var y = (CropCanvas.ActualHeight - height) / 2.0;
        return new Rect(x, y, width, height);
    }

    private void ApplySettingsToUi()
    {
        SetComboSelection(ThemeComboBox, _settings.Theme);
        SetComboSelection(BackgroundComboBox, _settings.Background);
        GridCheckBox.IsChecked = _settings.ShowGrid;
        GridMenuItem.IsChecked = _settings.ShowGrid;
        QualityTextBox.Text = _settings.JpegQuality.ToString(CultureInfo.InvariantCulture);
        ApplyTheme();
        ApplyViewportBackground();
        ApplyGrid();
    }

    private void PersistSettingsFromUi()
    {
        _settings.Theme = GetSelectedComboText(ThemeComboBox, _settings.Theme);
        _settings.Background = GetSelectedComboText(BackgroundComboBox, _settings.Background);
        _settings.ShowGrid = GridCheckBox.IsChecked == true;
        if (int.TryParse(QualityTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quality))
        {
            _settings.JpegQuality = Math.Clamp(quality, 1, 100);
            QualityTextBox.Text = _settings.JpegQuality.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void ApplyTheme()
    {
        var dark = string.Equals(_settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
        Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(dark ? Color.FromRgb(31, 36, 41) : Color.FromRgb(243, 245, 247));
        Application.Current.Resources["PanelBackgroundBrush"] = new SolidColorBrush(dark ? Color.FromRgb(42, 48, 54) : Colors.White);
        Application.Current.Resources["TextBrush"] = new SolidColorBrush(dark ? Color.FromRgb(235, 239, 242) : Color.FromRgb(30, 37, 43));
        Application.Current.Resources["MutedTextBrush"] = new SolidColorBrush(dark ? Color.FromRgb(168, 178, 188) : Color.FromRgb(102, 113, 125));
        ApplyViewportBackground();
    }

    private void ApplyViewportBackground()
    {
        var brush = _settings.Background switch
        {
            "White" => new SolidColorBrush(Colors.White),
            "Graphite" => new SolidColorBrush(Color.FromRgb(45, 49, 54)),
            _ => new SolidColorBrush(Color.FromRgb(220, 226, 232))
        };

        Application.Current.Resources["ViewportBrush"] = brush;
    }

    private void ApplyGrid()
    {
        GridOverlay.Visibility = _settings.ShowGrid && _document is not null ? Visibility.Visible : Visibility.Collapsed;
        GridOverlay.Fill = CreateGridBrush();
    }

    private static DrawingBrush CreateGridBrush()
    {
        var group = new DrawingGroup();
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(31, 111, 143)), 0.8);
        group.Children.Add(new GeometryDrawing(null, pen, new LineGeometry(new Point(0, 0), new Point(32, 0))));
        group.Children.Add(new GeometryDrawing(null, pen, new LineGeometry(new Point(0, 0), new Point(0, 32))));

        return new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 32, 32),
            ViewportUnits = BrushMappingMode.Absolute
        };
    }

    private Color GetRotationBackground()
    {
        return _settings.Background switch
        {
            "Graphite" => Color.FromRgb(45, 49, 54),
            _ => Colors.White
        };
    }

    private static void SetComboSelection(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static string GetSelectedComboText(ComboBox comboBox, string fallback)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? fallback;
    }

    private void ShowHelp()
    {
        const string help =
            "Управление:\n" +
            "Ctrl+O - открыть файл\n" +
            "Ctrl+S - сохранить\n" +
            "Ctrl+Shift+S - сохранить как\n" +
            "Ctrl+Left / Ctrl+Right - поворот на -90 / +90\n" +
            "H / V - отражение по горизонтали / вертикали\n" +
            "Обрезка - включить режим и выделить область мышью\n\n" +
            "Командная строка GUI:\n" +
            "RasterRotation.Wpf.exe C:\\Images\\photo.jpg\n\n" +
            "Консольная утилита:\n" +
            "RasterRotation.Cli.exe input.jpg --rotate 90 --flip h --output result.jpg";

        MessageBox.Show(this, help, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenExecuted(object sender, ExecutedRoutedEventArgs e) => OpenClick(sender, e);

    private void SaveExecuted(object sender, ExecutedRoutedEventArgs e) => SaveClick(sender, e);

    private void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e) => SaveAsClick(sender, e);

    private void HelpExecuted(object sender, ExecutedRoutedEventArgs e) => ShowHelp();

    private void FlipHorizontalExecuted(object sender, ExecutedRoutedEventArgs e) => FlipHorizontalClick(sender, e);

    private void FlipVerticalExecuted(object sender, ExecutedRoutedEventArgs e) => FlipVerticalClick(sender, e);

    private void RotateLeftExecuted(object sender, ExecutedRoutedEventArgs e) => RotateLeftClick(sender, e);

    private void RotateRightExecuted(object sender, ExecutedRoutedEventArgs e) => RotateRightClick(sender, e);
}
