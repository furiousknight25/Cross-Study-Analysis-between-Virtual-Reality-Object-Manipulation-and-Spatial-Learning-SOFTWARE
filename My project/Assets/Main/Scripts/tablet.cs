using TMPro;
using UnityEngine;

public class Tablet : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;

    void ShowText()
    {
        textMeshPro.gameObject.SetActive(true);     
    }

    void hideText()
    {
        textMeshPro.gameObject.SetActive(false);     
    }

    void spawn_items()
    {
        //spawn items
    }


}
