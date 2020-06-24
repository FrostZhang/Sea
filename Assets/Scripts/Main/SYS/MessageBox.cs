using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MessageBox : IPanel
{
    private static MessageBox ins;
    public static MessageBox Ins
    {
        get
        {
            if (ins == null)
            {
                ins = GameApp.Ins.ui.LoadPanelNow<MessageBox>(CanvasType.Top);
            }
            return ins;
        }
    }

    public Transform Tr { get; set; }

    [SerializeField]
    private GameObject m_Dropdown;
    private GameObject m_Blocker;

    private GameObject btns;
    private Button btnok;
    private Button btncancel;
    private RectTransform contentRect;
    private Text content;
    private Text title;
    List<MesSetting> popMesSettings;


    void IPanel.Start()
    {
        popMesSettings = new List<MesSetting>();
        contentRect = Tr.Find("Content") as RectTransform;
        btns = Tr.Find("Content/btns").gameObject;
        btns.SetActive(true);
        btnok = btns.transform.Find("OK").GetComponent<Button>();
        btncancel = btns.transform.Find("Cancel").GetComponent<Button>();
        content = Tr.Find("Content/content").GetComponent<Text>();
        title = Tr.Find("Content/title").GetComponent<Text>();
        Tr.gameObject.SetActive(false);
    }

    Coroutine coroutineShow;
    public void ShowMessage(MesSetting mesSetting)
    {
        if (Tr.gameObject.activeSelf)
        {
            popMesSettings.Add(mesSetting);
        }
        else
        {
            Tr.gameObject.SetActive(true);
            if (coroutineShow != null)
                GameApp.Ins.StopCoroutine(coroutineShow);
            coroutineShow = GameApp.Ins.StartCoroutine(ShowMessageDelay(mesSetting));
        }
    }

    public void CloseMessage()
    {
        if (popMesSettings.Count > 0)
            ShowNext();
        else
            Tr.gameObject.SetActive(false);
    }

    private void ShowNext()
    {
        if (coroutineShow != null)
            GameApp.Ins.StopCoroutine(coroutineShow);
        coroutineShow = GameApp.Ins.StartCoroutine(ShowMessageDelay(popMesSettings[0]));
        popMesSettings.RemoveAt(0);
    }

    IEnumerator ShowMessageDelay(MesSetting mesSetting)
    {
        Tr.SetAsLastSibling();
        btnok.gameObject.SetActive(false);
        btncancel.gameObject.SetActive(false);

        content.text = mesSetting.content;
        title.text = mesSetting.title;
        float h = content.preferredHeight;
        yield return null;
        //AjHeight(h, mesSetting.btnType != BtnType.Non);

        RegistBtn(mesSetting);
    }

    private void RegistBtn(MesSetting mesSetting)
    {
        if (mesSetting.btnType != BtnType.Non)
        {
            if (mesSetting.btnType == BtnType.Ok)
            {
                btnok.gameObject.SetActive(true);
                if (mesSetting.btnoktext != null)
                    btnok.GetComponentInChildren<Text>().text = mesSetting.btnoktext;
                btnok.onClick.RemoveAllListeners();
                btnok.onClick.AddListener(() =>
                {
                    InvokeMes(mesSetting.action, 1);
                });
                btncancel.gameObject.SetActive(false);
            }
            else if (mesSetting.btnType == BtnType.Cancel)
            {
                btnok.gameObject.SetActive(true);
                btncancel.gameObject.SetActive(true);
                if (mesSetting.btnoktext != null)
                    btnok.GetComponentInChildren<Text>().text = mesSetting.btnoktext;
                if (mesSetting.btncanceltext != null)
                    btncancel.GetComponentInChildren<Text>().text = mesSetting.btncanceltext;
                btnok.onClick.RemoveAllListeners();
                btnok.onClick.AddListener(() =>
                {
                    InvokeMes(mesSetting.action, 1);
                });
                btncancel.onClick.RemoveAllListeners();
                btncancel.onClick.AddListener(() =>
                {
                    InvokeMes(mesSetting.action, 0);
                });
            }
        }
    }

    private void InvokeMes(System.Action<int> act, int i)
    {
        if (act != null)
            act.Invoke(i);
        if (popMesSettings.Count > 0)
            ShowNext();
        else
            Tr.gameObject.SetActive(false);
    }

    private void AjHeight(float textHeight, bool hasBtn)
    {
        content.rectTransform.sizeDelta = new Vector2(content.rectTransform.sizeDelta.x, textHeight);
        if (hasBtn)
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, content.rectTransform.sizeDelta.y + 70);
        }
        else
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, content.rectTransform.sizeDelta.y + 50);
        }
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
        //Button button = gameObject.AddComponent<Button>();
        //button.onClick.AddListener(Hide);
        return gameObject;
    }

    void IPanel.OnDestroy()
    {

    }
}


public class MesSetting
{
    public string content;
    public string title;

    public BtnType btnType = BtnType.Non;

    public System.Action<int> action;

    public string btnoktext;
    public string btncanceltext;

}

public enum BtnType
{
    Non = 1,
    Ok = 4,
    Cancel = 8,
}