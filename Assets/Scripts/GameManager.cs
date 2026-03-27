using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Главный менеджер игры. Управляет состоянием, счетом и условиями победы.
/// Реализован как синглтон для удобного доступа из других скриптов.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Настройки игры")]
    public List<Color> availableColors; // Палитра доступных цветов для уровня
    public int totalZones;              // Общее количество зон (заполняется автоматически или вручную)
    
    private int coloredZonesCount = 0;
    private bool isGameComplete = false;

    void Awake()
    {
        // Синглтон паттерн для доступа из любого места
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Раскомментировать если нужно между сценами
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Если totalZones не задан, пробуем посчитать зоны на сцене
        if (totalZones == 0)
        {
            ColorZone[] zones = FindObjectsOfType<ColorZone>();
            totalZones = zones.Length;
            Debug.Log("Автоматически найдено зон: " + totalZones);
        }
        
        Debug.Log("Игра началась! Доступные цвета: " + availableColors.Count + ", Всего зон: " + totalZones);
    }

    /// <summary>
    /// Вызывается из ColorZone при успешном закрашивании
    /// </summary>
    public void OnZoneCompleted(int zoneId)
    {
        coloredZonesCount++;
        Debug.Log("Зона закрашена! Прогресс: " + coloredZonesCount + "/" + totalZones);
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (coloredZonesCount >= totalZones && !isGameComplete)
        {
            isGameComplete = true;
            Debug.Log("ПОБЕДА! Все зоны закрашены.");
            ShowVictoryScreen();
        }
    }

    private void ShowVictoryScreen()
    {
        // Логика показа UI победы
        Debug.Log("Показываем экран победы!");
        
        // Здесь можно:
        // - Включить панель победы
        // - Запустить эффект конфетти
        // - Сохранить прогресс
        // - Воспроизвести звук победы
    }

    /// <summary>
    /// Вызывается из ImageProcessor после обработки пользовательского фото.
    /// Здесь можно запустить процедуру генерации зон на основе текстуры.
    /// </summary>
    public void OnUserImageLoaded(Texture2D processedTexture)
    {
        Debug.Log("Пользовательское изображение загружено. Размер: " + processedTexture.width + "x" + processedTexture.height);
        
        // TODO: Здесь должна быть логика генерации игровых зон (спрайтов с коллайдерами)
        // на основе черных линий текстуры processedTexture.
        // Это сложная задача, требующая алгоритмов поиска связных областей (Flood Fill) 
        // или использования готовых ассетов для авто-трассировки.
        
        // Для простого варианта можно просто отобразить текстуру как фон,
        // а зоны расставить вручную или использовать упрощенную логику.
    }
}
