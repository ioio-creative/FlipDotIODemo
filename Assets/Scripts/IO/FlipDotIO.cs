using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SocketIO;
using System;

public class FlipDotIO : MonoBehaviour
{

    [Header("Pre-set settings")]
    [SerializeField]
    private SocketIOComponent IoComponent;
    [SerializeField]
    private string mobileBaseUrl;


    [Header("Data need to sync with IO")]
    [SerializeField]
    private Int32[] TexturePixels_Gray;

    [SerializeField]
    private string emitTextureIOEventName;


    // Start is called before the first frame update
    void Start()
    {
        IoComponent.On("connect", OnConnected);
        // using the ack from "createRoom", no need a seperate eventListener
        // IoComponent.On("roomCreated", OnRoomCreated);

    }

    // can call restart if want generate a new room id
    public void Restart()
    {
        IoComponent.On("disconnect", Reconnect);
        IoComponent.Close();
    }


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Keypad0))
        {
            //IoComponent.Emit("changeStage", JSONObject.Create(gameStatus.idle.ToString()));
        }
        else if (Input.GetKeyUp(KeyCode.F5))
        {
            Debug.Log("F5");
            Restart();
        }
    }

    private JSONObject toJsonObject(string[][] dataArray)
    {
        Dictionary<string, string> dat = new Dictionary<string, string>();
        Array.ForEach(dataArray, keyValue =>
        {
            dat.Add(keyValue[0], keyValue[1]);
        });
        return JSONObject.Create(dat);
    }


    private JSONObject GetChangeStageData(int status)
    {
        return toJsonObject(new String[][] { new String[] { "data", status.ToString() } });
    }

    //private JSONObject GetPixelObject(int[] pixels)
    //{
    //    var arrObj = new JSONObject(JSONObject.Type.ARRAY);
        
    //}


    // 
    private void OnConnected(SocketIOEvent obj)
    {
        Dictionary<string, string> dat = new Dictionary<string, string>();

        Guid guid = Guid.NewGuid();

        dat.Add("roomId", guid.ToString());

        IoComponent.Emit("createRoom", JSONObject.Create(dat), OnCreateRoomAck);

        IoComponent.Emit("changeStage", GetChangeStageData(0));

    }

    private void OnCreateRoomAck(JSONObject obj)
    {
        string newRoomId = "";
        float totalDistance = 0;
        float accumulatedDistance = 0;
        float distanceMultiplier = 0;
        int totalVisitFromServer = 0;
        obj[0].GetField(ref newRoomId, "data");
        obj[0].GetField(ref totalDistance, "totalDistance");
        obj[0].GetField(ref accumulatedDistance, "accumulatedDistance");
        obj[0].GetField(ref distanceMultiplier, "distanceMultiplier");
        obj[0].GetField(ref totalVisitFromServer, "totalVisit");

    }



    // reconnect logic
    private void Reconnect(SocketIOEvent obj)
    {
        Debug.Log("Reconnect");
        IoComponent.Off("disconnect", Reconnect);
        IoComponent.Connect();
    }



    #region Emit SocketEvent

    public void EmitTexture(Int32[] pixels)
    {
        //IoComponent.Emit(emitTextureIOEventName, pixels);

        Debug.Log(emitTextureIOEventName);
    }

    #endregion
}

