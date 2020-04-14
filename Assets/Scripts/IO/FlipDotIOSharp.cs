using System;
using System.Timers;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Linq;

public class FlipDotIOSharp
{
   
    // public ReceiveParser rp;
    public Timer timer;
    private SerialPort sp;
    private byte[][] msg;
    private int[,,] panels;
    
    public async Task<object> Invoke(dynamic input)
    {
        panels = new int[,,] {
          { {0, 7}, {28, 7} },
          { {0, 0}, {28, 7} },
          { {28, 7}, {28, 7} },
          { {28, 0}, {28, 7} },
          { {0, 21}, {28, 7} },
          { {0, 14}, {28, 7} },
          { {28, 21}, {28, 7} },
          { {28, 14}, {28, 7} },
        };
        msg = new byte[][] {
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
        string[] ports = SerialPort.GetPortNames();
        sp = new SerialPort("COM2", 57600, Parity.None, 8, StopBits.One);
        sp.Open();
        timer = new Timer(input.interval);
        timer.Elapsed += (Object source, ElapsedEventArgs e) => {
            for (int i = 0; i < msg.Length; i++)
            {
                Send((byte)(i + 1), msg[i]);
            }
        };
        timer.Enabled = true;
        return (Func<object, Task<object>>)(async (object data) => {
            // string dataString = data.ToString();
            // char[] charArray = dataString.ToCharArray();
            // int[] intArray = charArray.Select(a => a - '0').ToArray();
            int[] intArray = (data as object[]).Select(n => Convert.ToInt32(n)).ToArray();
            Build_message(intArray);
            return null;
        });
    }

    private void Send(byte screen_id, byte[] data, bool refresh = true)
    {
        byte[] formattedMsg = Format_message(screen_id, data, refresh);
        sp.Write(formattedMsg, 0, formattedMsg.Length);
    }

    private void Build_message(int[] fullImg)
    {
        if (fullImg.Length != 1568) return;
        // byte[][] result = new byte[panels.Length][];
        // Console.Write(fullImg[0].ToString());
        // Console.WriteLine(fullImg[1].ToString());
        for (int i = 0; i < panels.Length; i++)
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
                    //if (fullImg[x + (ys + y) * 56 ] == (int)0) {
                    // Console.WriteLine((x + (ys + y) * 56).ToString());
                    //}
                    byte pixel = (fullImg[x + (ys + y) * 56] == (int)0 ? (byte)0x00 : (byte)0x01);
                    cell = (byte)(cell << 1);
                    cell = (byte)(cell | pixel);
                }
                // result[i][x-xs] = cell;
                msg[i][x - xs] = cell;
            }
        }
        // msg = result;
    }
    
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

