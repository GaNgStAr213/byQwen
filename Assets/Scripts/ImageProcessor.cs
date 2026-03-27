using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

/// <summary>
/// Скрипт для загрузки, обработки и адаптации пользовательских изображений
/// под формат игры "Раскраска по цветам".
/// 
/// Функционал:
/// 1. Загрузка из галереи или камеры.
/// 2. Изменение размера (ресайз) для оптимизации.
/// 3. Постеризация (уменьшение количества цветов) для стилизации.
/// 4. Простое выделение границ (Edge Detection) для создания контуров.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ImageProcessor : MonoBehaviour
{
    [Header("Настройки обработки")]
    [Tooltip("Максимальная ширина итогового изображения (для производительности)")]
    public int maxResolutionWidth = 512;
    
    [Tooltip("Количество уровней яркости при постеризации (чем меньше, тем мультяшнее)")]
    [Range(2, 8)]
    public int posterizationLevels = 4;

    [Tooltip("Порог чувствительности для выделения границ (0-1)")]
    [Range(0.05f, 0.5f)]
    public float edgeThreshold = 0.15f;

    [Header("UI Элементы (опционально)")]
    public Button loadButton;
    public Button captureButton;
    public GameObject processingPanel; // Панель "Обработка..."

    private RawImage displayImage;
    private Texture2D currentTexture;

    void Awake()
    {
        displayImage = GetComponent<RawImage>();
        
        if (loadButton != null)
            loadButton.onClick.AddListener(() => StartCoroutine(LoadFromGallery()));
            
        if (captureButton != null)
            captureButton.onClick.AddListener(() => StartCoroutine(CaptureFromCamera()));
    }

    /// <summary>
    /// Запуск процесса загрузки из галереи (работает на мобильных устройствах)
    /// Примечание: Для работы требуется плагин нативной галереи (например, NativeGallery)
    /// </summary>
    public IEnumerator LoadFromGallery()
    {
        Debug.Log("Запрос доступа к галерее...");
        
        #if UNITY_ANDROID || UNITY_IOS
        // Здесь должен быть вызов плагина, например NativeGallery
        // Для примера эмулируем задержку и лог
        yield return new WaitForSeconds(0.5f);
        Debug.Log("На мобильных устройствах здесь откроется системный диалог выбора фото.");
        // В реальном проекте раскомментировать и настроить:
        /*
        NativeGallery.Permission permission = NativeGallery.CheckPermission();
        if (permission == NativeGallery.Permission.ShouldRequest)
            permission = NativeGallery.RequestPermission();
        
        if (permission == NativeGallery.Permission.Granted)
        {
            string path = NativeGallery.LoadImageFromGallery();
            if (!string.IsNullOrEmpty(path))
                yield return StartCoroutine(ProcessImagePath(path));
        }
        */
        #else
        Debug.Log("В редакторе Unity загрузка из галереи эмулируется через файл.");
        // Для тестов в редакторе можно открыть стандартный диалог Windows/Mac
        #endif
        
        yield return null;
    }

    /// <summary>
    /// Запуск процесса съемки на камеру
    /// </summary>
    public IEnumerator CaptureFromCamera()
    {
        if (!Application.isMobilePlatform && !WebCamTexture.devices.Any())
        {
            Debug.LogWarning("Камера недоступна или не найдена.");
            yield break;
        }

        yield return new WaitForEndOfFrame();

        WebCamTexture webCamTexture = new WebCamTexture();
        webCamTexture.Play();

        // Ждем пока камера не будет готова
        while (!webCamTexture.didUpdateThisFrame)
        {
            yield return null;
        }

        // Делаем снимок
        Texture2D snap = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        snap.SetPixels(webCamTexture.GetPixels());
        snap.Apply();

        webCamTexture.Stop();
        Destroy(webCamTexture);

        // Сохраняем во временный файл и обрабатываем
        string path = Path.Combine(Application.temporaryCachePath, "temp_snap.png");
        File.WriteAllBytes(path, snap.EncodeToPNG());
        Destroy(snap);

        yield return StartCoroutine(ProcessImagePath(path));
    }

    /// <summary>
    /// Основной пайплайн обработки изображения по пути
    /// </summary>
    private IEnumerator ProcessImagePath(string path)
    {
        if (processingPanel != null) processingPanel.SetActive(true);

        // Даем интерфейсу обновиться перед тяжелой операцией
        yield return null;

        if (!File.Exists(path))
        {
            Debug.LogError($"Файл не найден: {path}");
            if (processingPanel != null) processingPanel.SetActive(false);
            yield break;
        }

        byte[] fileData = File.ReadAllBytes(path);
        currentTexture = new Texture2D(2, 2);
        
        if (!currentTexture.LoadImage(fileData))
        {
            Debug.LogError("Не удалось загрузить изображение!");
            if (processingPanel != null) processingPanel.SetActive(false);
            yield break;
        }

        Debug.Log($"Изображение загружено: {currentTexture.width}x{currentTexture.height}. Начинаем обработку...");

        // 1. Ресайз
        Texture2D resized = ResizeTexture(currentTexture, maxResolutionWidth);
        Destroy(currentTexture);

        // 2. Постеризация (упрощение цветов)
        Texture2D posterized = Posterize(resized, posterizationLevels);
        Destroy(resized);

        // 3. Выделение границ для создания "раскраски"
        // Возвращает ч/б изображение с черными линиями контуров
        Texture2D finalResult = DetectEdges(posterized, edgeThreshold);
        
        Destroy(posterized);

        // Отображение результата
        displayImage.texture = finalResult;
        
        // Подгонка аспекта, если есть компонент AspectRatioFitter
        var fitter = GetComponent<AspectRatioFitter>();
        if (fitter != null)
            fitter.aspectRatio = (float)finalResult.width / finalResult.height;

        // Передача результата в GameManager для дальнейшей логики
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUserImageLoaded(finalResult);
        }

        if (processingPanel != null) processingPanel.SetActive(false);
        
        Debug.Log("Изображение успешно обработано и готово к игре!");
    }

    /// <summary>
    /// Изменяет размер текстуры, сохраняя пропорции, ограничивая максимальной шириной
    /// Использует алгоритм ближайшего соседа для скорости
    /// </summary>
    private Texture2D ResizeTexture(Texture2D source, int targetMaxWidth)
    {
        float ratio = (float)source.height / source.width;
        int newWidth = targetMaxWidth;
        int newHeight = Mathf.RoundToInt(targetMaxWidth * ratio);

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        
        Color[] pixels = source.GetPixels();
        Color[] newPixels = new Color[newWidth * newHeight];

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int srcX = Mathf.FloorToInt((float)x / newWidth * source.width);
                int srcY = Mathf.FloorToInt((float)y / newHeight * source.height);
                
                srcX = Mathf.Clamp(srcX, 0, source.width - 1);
                srcY = Mathf.Clamp(srcY, 0, source.height - 1);

                newPixels[y * newWidth + x] = pixels[srcY * source.width + srcX];
            }
        }

        result.SetPixels(newPixels);
        result.Apply();
        return result;
    }

    /// <summary>
    /// Уменьшает количество оттенков каждого канала цвета (постеризация)
    /// Это создает эффект "мультяшности" и упрощает последующее выделение границ
    /// </summary>
    private Texture2D Posterize(Texture2D source, int levels)
    {
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] pixels = source.GetPixels();
        Color[] newPixels = new Color[pixels.Length];

        float stepDivisor = levels - 1;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            c.r = Mathf.Round(c.r * stepDivisor) / stepDivisor;
            c.g = Mathf.Round(c.g * stepDivisor) / stepDivisor;
            c.b = Mathf.Round(c.b * stepDivisor) / stepDivisor;
            newPixels[i] = c;
        }

        result.SetPixels(newPixels);
        result.Apply();
        return result;
    }

    /// <summary>
    /// Детектор границ на основе оператора Собеля.
    /// Преобразует изображение в черно-белый контурный рисунок.
    /// </summary>
    private Texture2D DetectEdges(Texture2D source, float threshold)
    {
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] pixels = source.GetPixels();
        Color[] newPixels = new Color[pixels.Length];

        int width = source.width;
        int height = source.height;

        // Ядра Собеля
        int[,] gx = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] gy = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float rX = 0, gX = 0, bX = 0;
                float rY = 0, gY = 0, bY = 0;

                // Свертка ядра с пикселями
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        Color p = pixels[(y + ky) * width + (x + kx)];
                        
                        rX += p.r * gx[ky + 1, kx + 1];
                        gX += p.g * gx[ky + 1, kx + 1];
                        bX += p.b * gx[ky + 1, kx + 1];

                        rY += p.r * gy[ky + 1, kx + 1];
                        gY += p.g * gy[ky + 1, kx + 1];
                        bY += p.b * gy[ky + 1, kx + 1];
                    }
                }

                // Вычисляем величину градиента
                float rMag = Mathf.Sqrt(rX * rX + rY * rY);
                float gMag = Mathf.Sqrt(gX * gX + gY * gY);
                float bMag = Mathf.Sqrt(bX * bX + bY * bY);

                float magnitude = (rMag + gMag + bMag) / 3.0f;

                // Если градиент выше порога -> это граница (черный цвет), иначе фон (белый)
                // Инверсия: 0f = черный, 1f = белый
                float val = magnitude > threshold ? 0f : 1f; 
                
                newPixels[y * width + x] = new Color(val, val, val, 1f);
            }
        }

        result.SetPixels(newPixels);
        result.Apply();
        return result;
    }
}
