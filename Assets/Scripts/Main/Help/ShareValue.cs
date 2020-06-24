using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShareValue  
{
    public object value;
}

public class ShareValue<T> : ShareValue
{
    T t;
    public T GetValue()
    {
        return t;
    }
    public void SetValue(T t)
    {
        this.t = t ;
    }
}

public class ShareInt : ShareValue<int>
{
    public static implicit operator int(ShareInt va)
    {
        return va.GetValue();
    }
}

public class ShareTransform : ShareValue<Transform>
{
    public static implicit operator Transform(ShareTransform va)
    {
        return va.GetValue();
    }
}

public class ShareLiTransform : ShareValue<List<Transform>>
{
    public static implicit operator List<Transform>(ShareLiTransform va)
    {
        return va.GetValue();
    }
}