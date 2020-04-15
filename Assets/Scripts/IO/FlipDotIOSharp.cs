using System;
using System.IO;
using System.IO.Ports;
using UnityEngine;

public class FlipDotIOSharp : MonoBehaviour
{

    [SerializeField]
    private string settingsFileName = "FlipdotSettings.json";

    private FlipDotSettings settings;
    private SerialPort sp;

    // msg to be send
    private byte[][] msg;

    private int dotsPerFlipdot = 0;

    private void Awake()
    {
        loadSettingsFile();
        connect();
    }
    private void loadSettingsFile()
    {
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, settingsFileName);
        string jsonString = File.ReadAllText(jsonFilePath);
        settings = JsonUtility.FromJson<FlipDotSettings>(jsonString);
        
        // initialize the message array
        msg = new byte[settings.panels.Length][];
        for (int i = 0; i < settings.panels.Length; i++)
        {
            dotsPerFlipdot += settings.panels[i].width * settings.panels[i].height;
            msg[i] = new byte[settings.panels[i].width];
        }
    }
    public void connect()
    {
        sp = new SerialPort(settings.comPort, settings.baudRate, settings.parity, settings.dataBits, settings.stopBits);
        sp.Open();
    }

    public void SendImage(int[] imageArray)
    {
        Build_message(imageArray);
        for (int i = 0; i < msg.Length; i++)
        {
            Send((byte)(i + 1), msg[i]);
        }
    }

    private void Send(byte screen_id, byte[] data, bool refresh = true)
    {
        byte[] formattedMsg = Format_message(screen_id, data, refresh);
        sp.Write(formattedMsg, 0, formattedMsg.Length);
    }

    private void Build_message(int[] fullImg)
    {
        // just simple check if the total data length = filpdot dots length
        // 56 x 28 = 1568
        // need to change later when flipdot hardware change
        if (fullImg.Length != dotsPerFlipdot) return;
        for (int i = 0; i < settings.panels.Length; i++)
        {
            PanelDimension panel = settings.panels[i];
            int xs = panel.startX; // panels[i][0];
            int ys = panel.startY; // panels[i][1];
            int w = panel.width;   // panels[i][2];
            int h = panel.height;  // panels[i][3];
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
}

