# Raster Rotation Tool

Учебный проект на C#/.NET: инструмент для просмотра и изменения растровых изображений. Основная логика вынесена в библиотеку `RasterRotation.Core`, поэтому ее можно переиспользовать из WPF, Console, WinForms, Avalonia или другого интерфейса на Windows.

## Возможности

- открытие файла из командной строки или через GUI;
- PNG, JPEG, TIFF, BMP, GIF и другие форматы, если для них установлен WIC-кодек Windows, например HEIF/HEIC;
- сохранение через тот же WIC-контейнер, что и исходный файл;
- обрезка прямоугольной области мышью;
- отражение по горизонтали и вертикали;
- поворот на `-90`, `+90`, `180`;
- свободное вращение на произвольный угол;
- сетка поверх изображения;
- отображение имени файла, размера, DPI, формата и текущего угла;
- пользовательские настройки: тема, фон рабочей области, сетка, качество JPEG;
- горячая клавиша `F1` открывает справку.

## Структура

```text
RasterRotation.sln
src/
  RasterRotation.Core/  # библиотека обработки и сохранения изображений
  RasterRotation.Wpf/   # графический интерфейс WPF
  RasterRotation.Cli/   # консольная утилита
```

Ключевые классы библиотеки:

- `IImageFileService`, `WicImageFileService` - загрузка и сохранение через Windows Imaging Component;
- `IImageTransformer`, `WicImageTransformer` - обрезка, отражение и поворот;
- `ImageDocument`, `ImageMetadata`, `ImageFormatInfo` - объектная модель изображения и метаданных.

## Сборка и запуск

Требуется Windows и актуальный .NET SDK с Windows Desktop workload.

```powershell
dotnet build .\RasterRotation.sln
dotnet run --project .\src\RasterRotation.Wpf -- "C:\Images\photo.jpg"
```

Консольные примеры:

```powershell
dotnet run --project .\src\RasterRotation.Cli -- "C:\Images\photo.jpg" --info
dotnet run --project .\src\RasterRotation.Cli -- "C:\Images\photo.jpg" --rotate 90 --overwrite
dotnet run --project .\src\RasterRotation.Cli -- "C:\Images\photo.png" --crop 20,20,640,480 --flip h --output "C:\Images\photo_crop.png"
dotnet run --project .\src\RasterRotation.Cli -- "C:\Images\scan.tiff" --rotate -3.5 --quality 95 --output "C:\Images\scan_fixed.tiff"
```

Для демонстрации командной строки также есть notebook: `docs/cli-demo.ipynb`.

Подробная русскоязычная карта архитектуры, файлов, точек входа и форматов находится в `docs/PROJECT_ROADMAP_RU.md`.

## Горячие клавиши

- `Ctrl+O` - открыть изображение;
- `Ctrl+S` - сохранить;
- `Ctrl+Shift+S` - сохранить как;
- `Ctrl+Left` / `Ctrl+Right` - поворот на `-90` / `+90`;
- `H` / `V` - отражение по горизонтали / вертикали;
- `F1` - справка.

## Проверка основных сценариев

1. Открыть PNG/JPEG/TIFF через `Ctrl+O`.
2. Включить режим `Обрезка`, выделить область мышью и сохранить файл.
3. Проверить `-90`, `+90`, `180`, отражение и свободный угол.
4. Включить/выключить сетку, сменить тему и фон, перезапустить приложение.
5. Выполнить CLI-команды из раздела выше и убедиться, что создаются файлы ожидаемого размера и формата.

Примечание по качеству: точное исходное качество JPEG не хранится как единое число, поэтому приложение сохраняет JPEG с пользовательским параметром качества, по умолчанию `92`.
