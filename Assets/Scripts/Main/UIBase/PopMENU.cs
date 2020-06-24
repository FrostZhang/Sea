using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopMENU : Button
{
    [SerializeField]
    private GameObject m_Dropdown;
    private GameObject m_Blocker;

    public class popHandel : UnityEvent<int, string> { };
    public popHandel OnPopClick = new popHandel();

    protected override void Awake()
    {
        if (!Application.isPlaying)
            return;
        if (m_Dropdown)
        {
            m_Dropdown.SetActive(false);
        }
        Button[] btns = m_Dropdown.GetComponentsInChildren<Button>();
        for (int i = 0; i < btns.Length; i++)
        {
            int n = i;
            btns[n].onClick.AddListener(() => { OnPopClick?.Invoke(n, btns[n].GetComponentInChildren<Text>().text); Hide(); });
        }
    }

    public void Set(int i)
    {
        var btns = m_Dropdown.GetComponentsInChildren<Button>();
        if (btns.Length > i)
        {
            btns[i].onClick.Invoke();
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Show();
    }

    public void Show()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        m_Blocker = CreateBlocker(canvas);
        m_Dropdown.SetActive(true);

    }

    protected virtual GameObject CreateBlocker(Canvas rootCanvas)
    {
        GameObject gameObject = new GameObject("Blocker");
        RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.SetParent(rootCanvas.transform, false);
        rectTransform.anchorMin = Vector3.zero;
        rectTransform.anchorMax = Vector3.one;
        rectTransform.sizeDelta = Vector2.zero;
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        Canvas component = m_Dropdown.GetComponent<Canvas>();
        canvas.sortingLayerID = component.sortingLayerID;
        canvas.sortingOrder = component.sortingOrder - 1;
        gameObject.AddComponent<GraphicRaycaster>();
        Image image = gameObject.AddComponent<Image>();
        image.color = Color.clear;
        Button button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(Hide);
        return gameObject;
    }

    private void Hide()
    {
        if (m_Dropdown != null)
        {
            if (m_Dropdown.activeSelf)
            {
                m_Dropdown.SetActive(false);
            }
        }
        if (m_Blocker != null)
        {
            DestroyBlocker(m_Blocker);
        }
        m_Blocker = null;
        Select();
    }

    protected virtual void DestroyBlocker(GameObject blocker)
    {
        Destroy(blocker);
    }
}
