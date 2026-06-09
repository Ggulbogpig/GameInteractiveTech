using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

[System.Serializable]
public class Detection
{
    public string className;
    public float x;
    public float z;
    public float vx;
    public float vz;
}

public class DynamicObstacleReceiver : MonoBehaviour
{
    TcpClient client;
    StreamReader reader;

    GameObject personCube;

    private GlobalConfiguration gc;

    private int dynamicObstacleIndex = -1;

    //private DynamicAPF_Redirector apfRedirector;
    private RedirectionManager redirectionManager;
    private HeatmapAPF_Redirector heatmapAPF;



    void Start()
    {
        gc = FindObjectOfType<GlobalConfiguration>();

        redirectionManager =
            FindObjectOfType<RedirectionManager>();

        if (redirectionManager != null)
        {
            heatmapAPF =
                redirectionManager.redirector
                as HeatmapAPF_Redirector;
        }

        Debug.Log(
            "RedirectionManager = "
            + redirectionManager);

        Debug.Log(
            "Actual Redirector = "
            + redirectionManager.redirector);

        Debug.Log(
            "HeatmapAPF = "
            + heatmapAPF);

        Debug.Log(
            "GC = "
            + gc);

        try
        {
            client =
                new TcpClient(
                    "127.0.0.1",
                    9999);

            reader =
                new StreamReader(
                    client.GetStream());

            Debug.Log(
                "Connected To YOLO");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Connection Failed : "
                + e.Message);
        }
    }

    //void Start()
    //{
    //    gc = FindObjectOfType<GlobalConfiguration>();
    //    redirectionManager = FindObjectOfType<RedirectionManager>();


    //    //heatmapRedirector =
    //    FindObjectOfType<HeatmapAPF_Redirector>();



    //    Debug.Log("RedirectionManager = " + redirectionManager);
    //    Debug.Log("Actual Redirector = " + redirectionManager.redirector);
    //    //Debug.Log("APF Redirector = " + apfRedirector);

    //    Debug.Log("GC = " + gc);
    //    //apfRedirector =
    //        FindObjectOfType<DynamicAPF_Redirector>();


    //    try
    //    {
    //        client = new TcpClient("127.0.0.1", 9999);
    //        reader = new StreamReader(client.GetStream());

    //        Debug.Log("Connected To YOLO");
    //    }
    //    catch (System.Exception e)
    //    {
    //        Debug.LogError("Connection Failed : " + e.Message);
    //    }
    //}
    void UpdateDynamicObstacle()
    {
        if (gc == null)
            return;

        var space =
            gc.physicalSpaces[0];

        Vector3 p =
            personCube.transform.position;

        float halfSize = 0.3f;

        List<Vector2> poly =
            new List<Vector2>()
        {
        new Vector2(
            p.x-halfSize,
            p.z-halfSize),

        new Vector2(
            p.x+halfSize,
            p.z-halfSize),

        new Vector2(
            p.x+halfSize,
            p.z+halfSize),

        new Vector2(
            p.x-halfSize,
            p.z+halfSize)
        };
        if (dynamicObstacleIndex < 0)
        {
            dynamicObstacleIndex =
                space.obstaclePolygons.Count;

            space.obstaclePolygons.Add(poly);
            Debug.Log(
                "After Add Polygon = "
                + space.obstaclePolygons.Count);

            //if (heatmapRedirector != null)
            //{
            //    heatmapRedirector.obstacleWeights.Add(100f);

            //    Debug.Log(
            //        "After Add Polygon = " + space.obstaclePolygons.Count
            //        + " / Heatmap Weight = " + heatmapRedirector.obstacleWeights.Count);
            //}
            //else
            //{
            //    Debug.LogError("Heatmap Redirector is null");
            //}
            //Debug.Log(
            //    "After Add Weight = "
            //    + apfRedirector.obstacleWeights.Count);
        }
        else
        {
            space.obstaclePolygons[
                dynamicObstacleIndex]
                = poly;
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

            Detection det =
                JsonUtility.FromJson<Detection>(msg);

            if (det.className == "person")
            {
                if (personCube == null)
                {
                    personCube =
                        GameObject.CreatePrimitive(
                            PrimitiveType.Cube);

                    personCube.name =
                        "Dynamic_Person_Obstacle";

                    personCube.transform.localScale =
                        new Vector3(
                            0.4f,
                            1.0f,
                            0.4f);
                }

                //----------------------------------
                // 실제 월드 좌표 사용
                //----------------------------------

                Vector2 pos =
                    new Vector2(
                        det.x,
                        det.z);

                Vector2 vel =
                    new Vector2(
                        det.vx,
                        det.vz);

                personCube.transform.position =
                    new Vector3(
                        det.x,
                        0.5f,
                        det.z);

                //----------------------------------
                // Heatmap APF에 전달
                //----------------------------------

                if (heatmapAPF != null)
                {
                    heatmapAPF.SetDynamicObstacle(
                        pos,
                        vel,
                        150f);
                }

                //----------------------------------
                // 기존 Polygon 갱신
                //----------------------------------

                UpdateDynamicObstacle();
            }
        }
    }

    //void Update()
    //{
    //    if (client == null)
    //        return;

    //    if (client.Available > 0)
    //    {
    //        string msg = reader.ReadLine();

    //        Debug.Log("YOLO = " + msg);

    //        Detection det = JsonUtility.FromJson<Detection>(msg);

    //        if (det.className == "person")
    //        {
    //            if (personCube == null)
    //            {
    //                personCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //                personCube.name = "Dynamic_Person_Obstacle";
    //                personCube.transform.localScale = new Vector3(0.4f, 1.0f, 0.4f);
    //            }

    //            //float x = (det.cx - 320f) / 100f;
    //            //float z = 2.0f;
    //            Vector2 pos =
    //                new Vector2(det.x, det.z);

    //            Vector2 vel =
    //                new Vector2(det.vx, det.vz);

    //            personCube.transform.position = new Vector3(x, 0.5f, z);

    //            UpdateDynamicObstacle();
    //        }
    //    }
    //}

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
