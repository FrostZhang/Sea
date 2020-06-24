using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Copyright (C) 2019 All Rights Reserved.
// Detail：SysRecord	UnDo ReDo	2019/9/24
// Version：1.0.0
public partial class SysRecord
{
    private List<IModify> unDoModifies;

    private List<IModify> reDoModifies;
    public SysRecord()
    {
        unDoModifies = new List<IModify>();
        reDoModifies = new List<IModify>();
    }
    public void Clear()
    {
        unDoModifies.Clear();
        reDoModifies.Clear();
    }
    public IModify Undo()
    {
        if (unDoModifies.Count > 0)
        {
            IModify modify = unDoModifies[unDoModifies.Count - 1];
            unDoModifies.Remove(modify);
            modify.UnDo();
            reDoModifies.Add(modify);
            if (reDoModifies.Count > 100)
            {
                reDoModifies.RemoveAt(0);
            }
            return modify;
        }
        return null;
    }

    public IModify ReDo()
    {
        if (reDoModifies.Count > 0)
        {
            IModify modify = reDoModifies[reDoModifies.Count - 1];
            reDoModifies.Remove(modify);
            modify.Execute();
            unDoModifies.Add(modify);
            if (unDoModifies.Count > 100)
            {
                unDoModifies.RemoveAt(0);
            }
            return modify;
        }
        return null;
    }

    public void Do(IModify modify)
    {
        modify.Execute();
        unDoModifies.Add(modify);
        if (unDoModifies.Count > 100)
        {
            unDoModifies.RemoveAt(0);
        }
    }
}

public partial class SysRecord
{
    public class TextChange : IModify
    {
        string cache;
        Text ui;
        public TextChange(Text ui, string newtext)
        {
            cache = newtext;
            this.ui = ui;
            Execute();
        }

        public void Execute()
        {
            string s = ui.text;
            ui.text = cache;
            cache = s;
        }

        public void UnDo()
        {
            Execute();
        }
    }

    public class Move : IModify
    {
        Vector3 cache;
        Transform tr;

        public Move(Transform tr, Vector3 newPos)
        {
            cache = newPos;
            this.tr = tr;
        }
        public void Execute()
        {
            Vector3 s = tr.position;
            tr.position = cache;
            cache = s;
        }
        public void UnDo()
        {
            Execute();
        }
    }

    public class Rot : IModify
    {
        Vector3 cache;
        Transform tr;

        public Rot(Transform tr, Vector3 newRo)
        {
            cache = newRo;
            this.tr = tr;
        }
        public void Execute()
        {
            Vector3 s = tr.eulerAngles;
            tr.eulerAngles = cache;
            cache = s;
        }
        public void UnDo()
        {
            Execute();
        }
    }
}