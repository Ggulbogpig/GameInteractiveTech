using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HeatmapVisualizer : MonoBehaviour
{
    public int gridX = 40;
    public int gridY = 40;
    public float cellSize = 0.25f;

    public float sigma = 0.8f;
    //public float weight = 5f;

    public List<float> obstacleWeights =
    new List<float>()
{
    30f,
    50f,
    80f,
    100f
};
    public float wallWeight = 120.0f;
    public Material heatCellMaterial;

    private GameObject[,] cells;
    private Vector2[,] cellCoords;

    public SingleSpace space;

    //void Start()
    //{

    //    space = FindObjectOfType<GlobalConfiguration>().physicalSpaces[0];
    //    GenerateGrid();
    //    UpdateHeatmap();


    //    Debug.Log("HEATMAP START");

    //}
    IEnumerator Start()
    {
        Debug.Log("HEATMAP WAIT");

        GlobalConfiguration gc = null;

        while (gc == null)
        {
            gc =
                FindObjectOfType
                <GlobalConfiguration>();

            yield return null;
        }

        while (
            gc.physicalSpaces == null
            ||
            gc.physicalSpaces.Count == 0)
        {
            Debug.Log(
                "Waiting physicalSpaces...");

            yield return null;
        }

        Debug.Log(
            "physicalSpaces ready");

        space =
            gc.physicalSpaces[0];

        Debug.Log(
            "Obstacle count = "
            + space.obstaclePolygons.Count);

        GenerateGrid();
        UpdateHeatmap();
        StartCoroutine(HeatmapLoop());

        Debug.Log("HEATMAP DONE");


    }



    void GenerateGrid()
    {
        float minX =
            float.MaxValue;
        float minY =
            float.MaxValue;
        float maxX =
            float.MinValue;
        float maxY =
            float.MinValue;

        foreach (var p in space.trackingSpace)
        {
            if (p.x < minX)
                minX = p.x;

            if (p.x > maxX)
                maxX = p.x;

            if (p.y < minY)
                minY = p.y;

            if (p.y > maxY)
                maxY = p.y;
        }
        gridX =
        Mathf.CeilToInt(
        (maxX - minX) / cellSize);

        gridY =
        Mathf.CeilToInt(
        (maxY - minY) / cellSize);

        cells = new GameObject[gridX, gridY];
        cellCoords = new Vector2[gridX, gridY];


        foreach (var p in space.trackingSpace)
        {
            if (p.x < minX)
                minX = p.x;

            if (p.y < minY)
                minY = p.y;
        }


        for (int x = 0; x < gridX; x++)
            for (int y = 0; y < gridY; y++)
            {
                GameObject cell =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Quad);
                Destroy(
                    cell.GetComponent<Collider>());

                cell.transform.SetParent(transform,false);

                cell.transform.localScale =
                    Vector3.one * cellSize;

                cell.transform.rotation =
                    Quaternion.Euler(90, 0, 0);

                float worldX =
                    minX +
                    x * cellSize;

                float worldY =
                    minY +
                    y * cellSize;

                cell.transform.localPosition =
                    new Vector3(
                        worldX,
                        0.01f,
                        worldY);

                cellCoords[x, y] =
                    new Vector2(
                        worldX,
                        worldY);



                var renderer =
                    cell.GetComponent<MeshRenderer>();

                renderer.material =
                    new Material(
                        heatCellMaterial);

                cells[x, y] = cell;
            }
    }

    void UpdateHeatmap()
    {
        float maxRisk = 0f;
        float[,] risks =
            new float[gridX, gridY];

        for (int x = 0; x < gridX; x++)
            for (int y = 0; y < gridY; y++)
            {
                Vector2 pos = cellCoords[x, y];



                float risk =
                    ComputeRisk(pos);

                risks[x, y] = risk;

                if (risk > maxRisk)
                    maxRisk = risk;
            }

        for (int x = 0; x < gridX; x++)
            for (int y = 0; y < gridY; y++)
            {
                //float r =
                //    risks[x, y] / maxRisk;
                float r =Mathf.Pow(risks[x, y] / maxRisk, 0.5f);

                Color c =
                    Color.Lerp(
                        Color.blue,
                        Color.red,
                        r);

                cells[x, y]
                    .GetComponent<MeshRenderer>()
                    .material.color = c;
            }

    }

    IEnumerator HeatmapLoop()
    {
        while (true)
        {
            if (space != null)
            {
                UpdateHeatmap();
            }

            yield return
                new WaitForSeconds(0.2f);
        }
    }

    //float ComputeRisk(Vector2 pos)
    //{
    //    float risk = 0f;

    //    foreach (var ob in space.obstaclePolygons)
    //    {
    //        float minDist =
    //            GetDistanceToPolygon(
    //                pos,
    //                ob);

    //        risk +=
    //            weight *
    //            Mathf.Exp(
    //                -(minDist * minDist) /
    //                (2 * sigma * sigma));
    //    }

    //    return risk;
    //}


    //장애물만 시각화
    //float ComputeRisk(Vector2 pos)
    //{
    //    float risk = 0f;

    //    for (
    //        int obIndex = 0;
    //        obIndex <
    //        space.obstaclePolygons.Count;
    //        obIndex++)
    //    {
    //        var ob =
    //            space.obstaclePolygons
    //            [obIndex];

    //        float minDist =
    //            GetDistanceToPolygon(
    //                pos,
    //                ob);

    //        float semanticWeight =
    //            obstacleWeights
    //            [obIndex];

    //        risk +=
    //            semanticWeight *
    //            Mathf.Exp(
    //                -(minDist * minDist)
    //                /
    //                (2 *
    //                 sigma *
    //                 sigma));
    //    }

    //    return risk;
    //}

    //벽까지 시각화
    float ComputeRisk(Vector2 pos)
    {
        float risk = 0f;

        // 1 obstacle risk
        for (
            int obIndex = 0;
            obIndex <
            space.obstaclePolygons.Count;
            obIndex++)
        {
            var ob =
                space.obstaclePolygons
                [obIndex];

            float minDist =
                GetDistanceToPolygon(
                    pos,
                    ob);

            float semanticWeight =
            (obIndex < obstacleWeights.Count)
            ? obstacleWeights[obIndex]
            : 100f;

            risk +=
                semanticWeight *
                Mathf.Exp(
                    -(minDist * minDist)
                    /
                    (2 *
                     sigma *
                     sigma));
        }

        // 2 wall risk
        float wallDist =
            GetDistanceToPolygon(
                pos,
                space.trackingSpace);

        risk +=
            wallWeight *
            Mathf.Exp(
                -(wallDist * wallDist)
                /
                (2 *
                 sigma *
                 sigma));

        return risk;
    }

    float GetDistanceToPolygon(
        Vector2 p,
        List<Vector2> poly)
    {
        float minDist =
            float.MaxValue;

        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b =
                poly[(i + 1) % poly.Count];

            Vector2 nearest =
                GetNearestPointOnSegment(
                    p, a, b);

            float d =
                Vector2.Distance(
                    p,
                    nearest);

            if (d < minDist)
                minDist = d;
        }

        return minDist;
    }

    Vector2 GetNearestPointOnSegment(
        Vector2 p,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab = b - a;

        float t =
            Vector2.Dot(
                p - a,
                ab) /
            Vector2.Dot(
                ab,
                ab);

        t = Mathf.Clamp01(t);

        return a + t * ab;
    }

}