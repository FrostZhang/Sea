using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

// Copyright (C) 2019 All Rights Reserved.
// Detail：Memory	Red	2019/10/24
// Version：1.0.0
public abstract class Helper  //内存读写核心
{
    [DllImportAttribute("kernel32.dll", EntryPoint = "ReadProcessMemory")]
    public static extern bool ReadProcessMemory
        (
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

    [DllImportAttribute("kernel32.dll", EntryPoint = "OpenProcess")]
    public static extern IntPtr OpenProcess
        (
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId
        );

    [DllImport("kernel32.dll")]
    private static extern void CloseHandle
        (
            IntPtr hObject
        );

    //写内存
    [DllImportAttribute("kernel32.dll", EntryPoint = "WriteProcessMemory")]
    public static extern bool WriteProcessMemory
        (
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            int[] lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesWritten
        );

    //获取窗体的进程标识ID
    public static int GetPid(string windowTitle)
    {
        int rs = 0;
        Process[] arrayProcess = Process.GetProcesses();
        foreach (Process p in arrayProcess)
        {
            if (p.MainWindowTitle.IndexOf(windowTitle) != -1)
            {
                rs = p.Id;
                break;
            }
        }

        return rs;
    }

    //根据进程名获取PID
    public static int GetPidByProcessName(string processName)
    {
        Process[] arrayProcess = Process.GetProcessesByName(processName);

        foreach (Process p in arrayProcess)
        {
            return p.Id;
        }
        return 0;
    }

    //根据窗体标题查找窗口句柄（支持模糊匹配）
    public static IntPtr FindWindow(string title)
    {
        Process[] ps = Process.GetProcesses();
        foreach (Process p in ps)
        {
            if (p.MainWindowTitle.IndexOf(title) != -1)
            {
                return p.MainWindowHandle;
            }
        }
        return IntPtr.Zero;
    }

    //读取内存中的值
    public static int ReadMemoryValue(int baseAddress, string processName)
    {
        try
        {
            byte[] buffer = new byte[4];
            IntPtr byteAddress = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0); //获取缓冲区地址
            IntPtr hProcess = OpenProcess(0x1F0FFF, false, GetPid(processName));
            ReadProcessMemory(hProcess, (IntPtr)baseAddress, byteAddress, 4, IntPtr.Zero); //将制定内存中的值读入缓冲区
            CloseHandle(hProcess);
            return Marshal.ReadInt32(byteAddress);
        }
        catch
        {
            return 0;
        }
    }

    //将值写入指定内存地址中
    public static void WriteMemoryValue(int baseAddress, string processName, int value)
    {
        IntPtr hProcess = OpenProcess(0x1F0FFF, false, GetPid(processName)); //0x1F0FFF 最高权限
        WriteProcessMemory(hProcess, (IntPtr)baseAddress, new int[] { value }, 4, IntPtr.Zero);
        CloseHandle(hProcess);
    }

    ////将值写入指定内存中
    //public void WriteMemory(int baseAdd, int value)
    //{
    //    Helper.WriteMemoryValue(baseAdd, processName, value);
    //}

    //private int baseAddress = 0x00111DEC; //游戏内存基址
    //private string processName = "QQ游戏 - 连连看角色版"; //游戏进程名字
    //int temp = 0; int sit = 0;

    //private void timer1_Tick(object sender, EventArgs e) //冻结时间
    //{
    //    int address = ReadMemoryValue(baseAddress);
    //    address += 0x49d8;

    //    if (checkBox1.Checked == true)
    //    {
    //        timer1.Enabled = true;
    //        WriteMemory(address, 700);
    //        label1.Text = "已冻结";

    //    }
    //    if (checkBox1.Checked != true) label1.Text = "未冻结";
    //}
}