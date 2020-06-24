using LitJson;
using System;
using System.Net;
using UnityEngine;

// Copyright (C) 2019 All Rights Reserved.
// Detail：MVC	MyChessboard	2019/9/24
// Version：1.0.0
public class MVC
{
    public static Action OnSysQuit;
    public static Action<Collider, bool> OnIndicatorTriger;
    public static Action<Vector3> OnClickGround;
    public static Action<Transform> OnClickItem;

    public static Action<float> OnSceneLoad;

    public static Action<int> OnIndicatorShow;
    public static Action<int, Vector3> OnConstructShow;

    public static Action<Transform> OnImageControllerDetailDelete;
    public static System.Action<string, JsonData> OnMqttData;

    public static Action OnFindMa;
    public static Action OnSystemQuit;
    public static System.Action<IPAddress, JsonData> OnUDPData;
}