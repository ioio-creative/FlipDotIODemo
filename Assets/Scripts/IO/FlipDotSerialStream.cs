using RJCP.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipDotSerialStream : MonoBehaviour
{
    public SerialPortStream sps;

    private void Awake()
    {
        sps = new SerialPortStream("COM1", 57600, 8, Parity.None, StopBits.One);

        sps.Open();
        //sps.
    }
}
