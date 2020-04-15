using System;
using System.Threading;
using System.IO;
using System.IO.Ports;
using UnityEngine;

[Serializable]
public struct PanelDimension
{
    public int startX;
    public int startY;
    public int width;
    public int height;
}


// filpdot config
[Serializable]
public struct FlipDotSettings
{
    public string comPort;          // = "COM1";
    public PanelDimension[] panels; /* = [
                                            {
                                               "startX": 0,
                                               "startY": 7,
                                               "width":  28,
                                               "height": 7
                                            },
                                            ...
                                         ]; */
    public int baudRate;            // = 57600;
    public Parity parity;           // = 0; // = Parity.None;
    public int dataBits;            // = 8;
    public StopBits stopBits;       // = 1; // = Stopbits.One;
    public int lineStride;          // panelColCount * dotsPerRow
}

public class FlipDotSerialPort : MonoBehaviour
{
    [SerializeField]
    private string settingsFileName = "FlipdotSettings.json";

    [SerializeField]
    private FlipDotSettings settings;

    [SerializeField]
    private int dotsInFlipdot;

    private SerialPort sp;
    private Thread sendThread;

    private void loadSettingsFile()
    {
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, settingsFileName);
        string jsonString = File.ReadAllText(jsonFilePath);
        settings = JsonUtility.FromJson<FlipDotSettings>(jsonString);
        dotsInFlipdot = 0;
        for (int i = 0; i < settings.panels.Length; dotsInFlipdot += settings.panels[i].width * settings.panels[i].height, i++) ;
    }
    private void OnEnable()
    {
        loadSettingsFile();
        if (sp == null || !sp.IsOpen)
        {
            sp = new SerialPort(settings.comPort, settings.baudRate, settings.parity, settings.dataBits, settings.stopBits);
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

    private byte[][] Build_message(int[] fullImg)
    {
        // just simple check if the total data length = filpdot dots length
        if (fullImg.Length != dotsInFlipdot) {
            return new byte[0][]; // prevent null handling outside
        }
        // initialize the message array
        byte[][] msg = new byte[settings.panels.Length][];
        for (int i = 0; i < settings.panels.Length; i++)
        {
            msg[i] = new byte[settings.panels[i].width];
            PanelDimension panel = settings.panels[i];
            int xs = panel.startX;
            int ys = panel.startY;
            int w = panel.width;
            int h = panel.height;
            for (int x = xs; x < xs + w; x++)
            {
                byte cell = 0;
                for (int y = h - 1; y > -1; y--)
                {
                    byte pixel = (fullImg[x + (ys + y) * settings.lineStride] == (int)0 ? (byte)0x00 : (byte)0x01);
                    cell = (byte)(cell << 1);
                    cell = (byte)(cell | pixel);
                }
                msg[i][x - xs] = cell;
            }
        }
        return msg;
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
        byte[][] msg = Build_message(imageData);
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
