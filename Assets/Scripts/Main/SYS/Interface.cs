using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public interface IGameController
{

}

//�������GameController,���泡��Destroy
public interface ICellControler
{
    void OnGameControllerDestroy();
}

public interface IModify
{
    void Execute();
    void UnDo();
}

public interface IPanel
{
    UnityEngine.Transform Tr { get; set; }
    void Start();
    void OnDestroy();
}

public interface IAppUpdate
{
    void Update();
}
public interface IAppFixUpdate
{
    void FixUpdate();
}
public interface ILateUpdate
{
    void LateUpdate();
}


public interface IUDP
{
    bool IsReady { get; set; }
    UdpClient Sendclient { get; }

    void Close();
    void FoundMa();
    Task Ini();
    Task Send(string jsonstring, IPAddress ip);
}

public interface IMqtt
{
    bool IsReady { get; }
    void CheckMqtt();
    void Close();
    void Ini();
    void Reported(string shadowname, string jsonstring);
}

public interface INetSurface
{
    void Close();
    UdpClient GetSendClient();
    void Ini();
    byte SendData(IPAddress ip, string mqttName, string reportname, int value);

    void SetUDPAviable(bool isAviable);
}

public interface IHelloWorld
{
    Task Load();
    void Call(string classname, string method, params object[] p);
    T Call<T>(string classname, string method, params object[] p);
    void OnDestroy();
}

public interface ITrPoolItem
{
    void Start(Transform tr);

    void OnEnterPool();
}