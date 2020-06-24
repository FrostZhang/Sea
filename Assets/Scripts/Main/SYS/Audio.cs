using UnityEngine;

// Copyright (C) 2019 All Rights Reserved.
// Detail：Voice	MyChessboard	2019/8/30
// Version：1.0.0
public class AudioController
{
    public AudioSource bg;
    public AudioSource sys;
    private float mVolume;
    private float bgVolume;
    private float sysVolume;
    public float MVolume
    {
        get {return  mVolume; }
        set 
        {
            mVolume = value;
            bg.volume = mVolume * bgVolume;
            sys.volume = mVolume * bgVolume;
        }
    }
    public float BgVolume { get { return bgVolume; }
        set { bgVolume = value;
            bg.volume = mVolume * bgVolume;
        } }
    public float EffVolume { get; set; }
    public float SysVolume { get { return sysVolume; } set { sysVolume = value;
            sys.volume = mVolume * bgVolume;
        }
    }

    public void Start()
    {
        var g1 = new GameObject();
        g1.transform.SetParent(GameApp.Ins.Tr);
        g1.name = "BGAudio";
        bg = g1.AddComponent<AudioSource>();
        var g2 = new GameObject();
        g2.transform.SetParent(GameApp.Ins.Tr);
        g2.name = "EffAudio";
        sys = g2.AddComponent<AudioSource>();
        bg.loop = false;

        Debug.Log("Audio ini");
    }

    public void PlayEffect(string effname)
    {
        //AudioSource.PlayClipAtPoint();
    }
}
