using System.IO;
using System.Net.Sockets;
using UnityEngine;

[System.Serializable]
public class Detection
{
    public string className;
    public float cx;
    public float cy;
}

public class DynamicObstacleReceiver : MonoBehaviour
{
    TcpClient client;
    StreamReader reader;

    GameObject personCube;

    void Start()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 9999);
            reader = new StreamReader(client.GetStream());

            Debug.Log("Connected To YOLO");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Connection Failed : " + e.Message);
        }
    }

    void Update()
    {
        if (client == null)
            return;

        if (client.Available > 0)
        {
            string msg = reader.ReadLine();

            Debug.Log("YOLO = " + msg);

            Detection det = JsonUtility.FromJson<Detection>(msg);

            if (det.className == "person")
            {
                if (personCube == null)
                {
                    personCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    personCube.name = "Dynamic_Person_Obstacle";
                    personCube.transform.localScale = new Vector3(0.4f, 1.0f, 0.4f);
                }

                float x = (det.cx - 320f) / 100f;
                float z = 2.0f;

                personCube.transform.position = new Vector3(x, 0.5f, z);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (reader != null)
            reader.Close();

        if (client != null)
            client.Close();
    }
}



//using System.Net.Sockets;
//using System.IO;
//using UnityEngine;

//[System.Serializable]
//public class Detection
//{
//    public string className;
//    public float cx;
//    public float cy;
//}

//public class DynamicObstacleReceiver
//    : MonoBehaviour
//{
//    private bool spawned = false;
//    GameObject personCube;

//    TcpClient client;

//    StreamReader reader;

//    void Start()
//    {
//        client =
//            new TcpClient(
//                "127.0.0.1",
//                9999);

//        reader =
//            new StreamReader(
//                client.GetStream());

//        Debug.Log(
//            "Connected To YOLO");
//    }

//    void Update()
//    {
//        if (client.Available > 0)
//        {
//            string msg =
//                reader.ReadLine();

//            Debug.Log(
//                "YOLO = "
//                + msg);

//            if (msg.Contains("\"person\""))
//            {
//                if (personCube == null)
//                {
//                    personCube =
//                        GameObject.CreatePrimitive(
//                            PrimitiveType.Cube);

//                    personCube.transform.position =
//                        new Vector3(
//                            cx * 0.01f,
//                            0.5f,
//                            cy * 0.01f);


//                    Debug.Log(
//                        "Person Cube Created");
//                }
//            }


//        }
//    }
//}
