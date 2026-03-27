using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт для кнопки выбора цвета в палитре.
/// Вешается на UI-кнопку или объект с Image.
/// </summary>
public class ColorPicker : MonoBehaviour, IPointerClickHandler
{
    [Header("Настройки цвета")]
    public Color colorValue = Color.white; // Цвет, который представляет эта кнопка
    
    private UnityEngine.UI.Image backgroundImage;
    private bool isSelected = false;

    void Awake()
    {
        // Пытаемся получить компонент Image для фона кнопки (для UI)
        backgroundImage = GetComponent<UnityEngine.UI.Image>();
    }

    void Start()
    {
        // Инициализация цвета при старте
        UpdateVisuals();
        SetSelected(false);
    }

    /// <summary>
    /// Вызывается при клике на кнопку цвета
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectColor();
    }

    /// <summary>
    /// Для не-UI объектов (SpriteRenderer)
    /// </summary>
    void OnMouseDown()
    {
        SelectColor();
    }

    private void SelectColor()
    {
        Debug.Log("Выбран цвет: " + colorValue);
        
        // Визуально выделяем выбранный цвет
        SetSelected(true);
        
        // Сообщаем GameManager о выборе цвета (через статическое свойство или событие)
        // В данной реализации используем простой подход через статическое поле
        ColorInputManager.SetCurrentColor(colorValue);
    }

    /// <summary>
    /// Устанавливает состояние выделения для кнопки
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Если это UI кнопка, можно добавить обводку или изменить масштаб
        if (backgroundImage != null && isSelected)
        {
            // Пример визуального выделения (можно улучшить)
            transform.localScale = Vector3.one * 1.15f;
        }
        else if (backgroundImage != null)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void UpdateVisuals()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = colorValue;
        }
        
        // Для SpriteRenderer (если используется не UI)
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorValue;
        }
    }
    
    /// <summary>
    /// Метод для установки цвета извне (если палитра генерируется кодом)
    /// </summary>
    public void SetColor(Color newColor)
    {
        colorValue = newColor;
        UpdateVisuals();
    }
}

/// <summary>
/// Простой менеджер для хранения текущего выбранного цвета.
/// Используется как статический класс для доступа из ZoneClickHandler.
/// </summary>
public static class ColorInputManager
{
    public static Color CurrentSelectedColor { get; private set; } = Color.clear;

    public static void SetCurrentColor(Color color)
    {
        CurrentSelectedColor = color;
        Debug.Log("Текущий активный цвет: " + color);
    }

    public static Color GetCurrentColor()
    {
        return CurrentSelectedColor;
    }
}
