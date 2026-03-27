using UnityEngine;

/// <summary>
/// Скрипт обработки кликов по зонам раскраски.
/// Вешается на каждую зону вместе с ColorZone и Collider2D.
/// </summary>
public class ZoneClickHandler : MonoBehaviour
{
    private ColorZone colorZone;

    void Start()
    {
        colorZone = GetComponent<ColorZone>();
        if (colorZone == null)
        {
            Debug.LogError("На объекте " + gameObject.name + " должен быть компонент ColorZone!");
        }
    }

    /// <summary>
    /// Обработка клика мышью или тапа по экрану (для 2D объектов с Collider2D)
    /// </summary>
    void OnMouseDown()
    {
        HandleInteraction();
    }

    private void HandleInteraction()
    {
        if (colorZone == null) return;

        // Получаем текущий выбранный игроком цвет через статический менеджер
        Color currentSelectedColor = ColorInputManager.GetCurrentColor();
        
        if (currentSelectedColor != Color.clear)
        {
            colorZone.TryColor(currentSelectedColor);
        }
        else
        {
            Debug.Log("Сначала выберите цвет из палитры!");
            // Здесь можно добавить визуальную подсказку игроку
        }
    }
}
