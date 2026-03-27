using UnityEngine;

/// <summary>
/// Логика отдельной зоны для раскрашивания.
/// Вешается на каждый сегмент рисунка вместе с Collider2D и SpriteRenderer.
/// </summary>
public class ColorZone : MonoBehaviour
{
    [Header("Настройки зоны")]
    public Color targetColor; // Правильный цвет для этой зоны
    public int zoneId;        // Уникальный номер зоны (опционально, для логики "по номерам")

    private SpriteRenderer spriteRenderer;
    private bool isColored = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("На объекте " + gameObject.name + " отсутствует компонент SpriteRenderer!");
        }
        
        // Инициализируем начальным цветом (светло-серый фон)
        if (!isColored && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.8f, 0.8f, 0.8f, 1f); 
        }
    }

    /// <summary>
    /// Попытка закрасить зону выбранным цветом
    /// Вызывается из ZoneClickHandler
    /// </summary>
    public void TryColor(Color selectedColor)
    {
        if (isColored) return; // Уже закрашено

        // Проверяем совпадение цветов с небольшим допуском
        if (ColorsMatch(selectedColor, targetColor))
        {
            ApplyColor(targetColor);
            
            // Сообщаем GameManager об успешном завершении зоны
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZoneCompleted(zoneId);
            }
        }
        else
        {
            // Опционально: эффект ошибки (мигание красным)
            Debug.Log("Неверный цвет для этой зоны! Ожидался: " + targetColor);
            StartCoroutine(FlashError());
        }
    }

    private void ApplyColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        isColored = true;
        Debug.Log("Зона " + zoneId + " закрашена!");
    }

    private bool ColorsMatch(Color c1, Color c2)
    {
        // Сравниваем цвета с допустимой погрешностью (epsilon)
        return Mathf.Abs(c1.r - c2.r) < 0.05f &&
               Mathf.Abs(c1.g - c2.g) < 0.05f &&
               Mathf.Abs(c1.b - c2.b) < 0.05f;
    }

    /// <summary>
    /// Простая анимация ошибки (мигание красным)
    /// </summary>
    private System.Collections.IEnumerator FlashError()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Для отладки: рисует контур зоны в редакторе
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.3f);
        
        // Рисуем квадрат вокруг объекта для визуализации
        Vector3 pos = transform.position;
        Vector3 scale = transform.lossyScale;
        
        Gizmos.DrawCube(pos, new Vector3(scale.x, scale.y, 0));
    }
}
