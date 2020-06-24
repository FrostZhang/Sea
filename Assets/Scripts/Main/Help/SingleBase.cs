using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleBase<T>
{
    static T instance = default(T);

    public static T GetInstance()
    {
        if (instance == null)
        {
            instance = (T)System.Reflection.Assembly.GetAssembly(typeof(T)).CreateInstance(typeof(T).ToString());
        }
        return instance;
    }
}