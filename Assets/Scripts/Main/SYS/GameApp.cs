using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class GameApp : MonoBehaviour
{
    public static GameApp Ins { get; private set; }
    public Transform Tr => transform;
    //public VersionController version;
    //public Language lan;
    public SceneController scene;
    public Surface ui;
    public AppConfig appConfig;
    public CashData cash;
    public new AppAudio audio;
    public new AppCamera camera;
    public INetSurface net;
    public IHelloWorld hello;
    public DBManager sqlitDB;

    //IsControllerIni 可去掉
    public bool IsControllerIni { get; private set; }
    public SysRecord sysRecord; //Undo Redo ceshi

    public IGameController GameController
    {
        get { return gameController; }
        set
        {
            if (gameController != value)
            {
                if (gameControllerinfos != null)
                {
                    foreach (var item in gameControllerinfos)
                    {
                        var cell = item.GetValue(gameController);
                        (cell as ICellControler)?.OnGameControllerDestroy();
                    }
                }
                gameController = value;
                if (value != null)
                    gameControllerinfos = value.GetType().GetFields();
                else
                    gameControllerinfos = null;
            }
        }
    }
    IGameController gameController;
    System.Reflection.FieldInfo[] gameControllerinfos;

    public void Awake()
    {
        if (Ins)
        {
            Destroy(Tr.gameObject); return;
        }
        else
        {
            Ins = this;
            DontDestroyOnLoad(Tr.gameObject);
        }

        //        BuglyAgent.ConfigDebugMode(true);
        //#if UNITY_IPHONE || UNITY_IOS
        //BuglyAgent.InitWithAppId ("Your App ID");
        //#elif UNITY_ANDROID
        //        BuglyAgent.InitWithAppId("312750c1a4");
        //#endif
        //        BuglyAgent.EnableExceptionHandler();

        //        Debuger.Level = appConfig.debugLv;
        //#if UNITY_EDITOR
        //        Debuger.plaform = 0;
        //#elif UNITY_ANDROID
        //       Debuger.plaform = 1;
        //#endif
        Check();
        Application.targetFrameRate = appConfig.fame;
    }

    private void Check()
    {
        IniController();
    }

    public void IniController()
    {
        ui = new Surface(Tr);
        //version = new VersionController();
        //await version.Start();
        //lan = new Language();
        //await lan.Start();
        audio = new AppAudio();
        audio.Start();
        scene = new SceneController();
        sysRecord = new SysRecord();
        cash = new CashData();
        camera = new AppCamera(Tr);
        //FAC fac = new FAC();
        //hello = FAC.container.ResolveKeyed<IHelloWorld>("HelloWorld");
        //await hello.Load();
        IsControllerIni = true;
        if (!string.IsNullOrEmpty(appConfig.debugScene))
        {
            //await scene.LoadAsync(appConfig.debugScene);
        }

        sqlitDB = DBManager.GetInstance();
        sqlitDB.OpenConnect();
        //net = FAC.container.ResolveKeyed<INetSurface>("NetSurface");
        //ui.RemovePanel<UpdatePanel>();
    }

    #region UnityEvent
    public T GetControllerCompent<T>(string popName = null)
    {
        foreach (var item in gameControllerinfos)
        {
            if (!string.IsNullOrEmpty(popName) && item.Name != popName)
                continue;
            if (item.FieldType == typeof(T))
            {
                var value = item.GetValue(gameController);
                if (value == null)
                    Debug.Log(gameController.GetType().Name + " find " + item.FieldType.Name + " is null");
                else
                    return (T)value;
            }
        }
        return default(T);
    }
    public List<IAppUpdate> Updates { get; } = new List<IAppUpdate>();
    public List<IAppFixUpdate> FixeUpdates { get; } = new List<IAppFixUpdate>();
    public List<ILateUpdate> LateUpdates { get; } = new List<ILateUpdate>();
    void LateUpdate()
    {
        for (int i = 0; i < LateUpdates.Count; i++)
        {
            LateUpdates[i].LateUpdate();
        }
    }
    void FixedUpdate()
    {
        for (int i = 0; i < FixeUpdates.Count; i++)
        {
            FixeUpdates[i].FixUpdate();
        }
    }
    void Update()
    {
        for (int i = 0; i < Updates.Count; i++)
        {
            Updates[i].Update();
        }
    }

    void OnDestroy()
    {
        Ins = null;
        MVC.OnSysQuit?.Invoke();
        GameController = null;
        //version = null;
        //lan = null;
        scene = null;
        ui = null;
        appConfig = null;
        audio = null;
        cash = null;
        sysRecord = null;
        //hello.OnDestroy();
        GameController = null;
        Updates.Clear();
        FixeUpdates.Clear();
        sqlitDB.CloseDB();
        Debug.Log("Sys quit");
    }
    #endregion

}
