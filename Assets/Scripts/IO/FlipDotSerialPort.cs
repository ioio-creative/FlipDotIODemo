﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using UnityEngine;

public class FlipDotSerialPort : MonoBehaviour
{
    public string portName;
    
    public SerialPort sp;

    private Thread sendThread;

    // filpdot hardware conf.
    // {sx, sy}, {w, h}
    private int[,,] panels = new int[,,] {
        { {0, 7}, {28, 7} },
        { {0, 0}, {28, 7} },
        { {28, 7}, {28, 7} },
        { {28, 0}, {28, 7} },
        { {0, 21}, {28, 7} },
        { {0, 14}, {28, 7} },
        { {28, 21}, {28, 7} },
        { {28, 14}, {28, 7} },
    };

    // msg to be send
    private byte[][] msg = new byte[][] {
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 34, 62, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 42, 46, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 14,  8, 62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 46, 42, 58, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 42, 58, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  2, 62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        },
        new byte[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 42, 62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        }
    };

    private void Awake()
    {
    }

    private void OnEnable()
    {
        if (sp == null || !sp.IsOpen)
        {
            sp = new SerialPort(portName, 57600, Parity.None, 8, StopBits.One);
            sp.Open();
        }
        
    }

    private void OnDisable()
    {
        if (sp.IsOpen)
        {
            sp.Close();
            sp.Dispose();
        }

        if (sendThread != null && sendThread.IsAlive)
        {
            sendThread.Abort();
        }
    }

    private void Build_message(int[] fullImg)
    {
        // just simple check if the total data length = filpdot dots length
        // 56 x 28 = 1568
        // need to change later when flipdot hardware change
        if (fullImg.Length != 1568) return;
        for (int i = 0; i < 8; i++)
        {
            int xs = panels[i, 0, 0];
            int ys = panels[i, 0, 1];
            int w = panels[i, 1, 0];
            int h = panels[i, 1, 1];
            for (int x = xs; x < xs + w; x++)
            {
                byte cell = 0;
                for (int y = h - 1; y > -1; y--)
                {
                    byte pixel = (fullImg[x + (ys + y) * 56] == (int)0 ? (byte)0x00 : (byte)0x01);
                    cell = (byte)(cell << 1);
                    cell = (byte)(cell | pixel);
                }
                msg[i][x - xs] = cell;
            }
        }
    }

    // format the message 
    private byte[] Format_message(byte screen_id, byte[] data, bool refresh = true)
    {
        byte msg = 0x00;
        int dataLength = data.Length;
        switch (dataLength)
        {
            case 112:
                msg = (byte)(refresh ? 0x82 : 0x81);
                break;
            case 28:
                msg = (byte)(refresh ? 0x83 : 0x84);
                break;
            case 56:
                msg = (byte)(refresh ? 0x85 : 0x86);
                break;
        }
        // bytearray([0x80, msg, screen_id]) + data + bytearray([0x8F])
        byte[] result = new byte[1 + 1 + 1 + dataLength + 1];
        result[0] = 0x80;
        result[1] = msg;
        result[2] = screen_id;
        Buffer.BlockCopy(data, 0, result, 3, dataLength);
        result[3 + dataLength] = 0x8F;
        return result;
    }

    private void SendToScreen(byte screen_id, byte[] data, bool refresh = true)
    {
        byte[] formattedMsg = Format_message(screen_id, data, refresh);
        sp.Write(formattedMsg, 0, formattedMsg.Length);
    }

    private void SendingThread(int[] imageData)
    {
        Build_message(imageData);
        for (int i = 0; i < msg.Length; i++)
        {
            SendToScreen((byte)(i + 1), msg[i]);
        }
    }


    public void SendFlipDotImage(int[] imageArray)
    {
        if (sp.IsOpen)
        {
            sendThread = new Thread(() => SendingThread(imageArray));
            sendThread.IsBackground = true;
            sendThread.Start(); 
        }
    }
}