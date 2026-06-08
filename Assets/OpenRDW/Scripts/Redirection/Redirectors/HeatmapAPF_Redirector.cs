using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HeatmapAPF_Redirector : DynamicAPF_Redirector
{
    public float gridSize = 0.25f;
    public float riskSigma = 0.8f;
    //public float riskWeight = 5.0f;
    public float heatmapInfluence = 3.0f;

    public GameObject saccadeCue;
    public AudioSource warningAudio;

//0-1 사잇값 Threshold, 0이면 거의 안 꺾음, 0.5면 90도, 1이면 거의 다
    public float sgdTurnThreshold = 0.35f;
    public float soundTurnThreshold = 0.65f;
    public float resetTurnThreshold = 0.9f;

    public float sgdDuration = 0.15f;
    public float sgdCooldown = 1.5f;
    private float lastSgdTime = -999f;
    private float prevTurnNeed = 0f;

    public float sgdMultiplier = 1.8f;

    private bool isSGDWindow = false;

    public GameObject leftFlash;
    public GameObject rightFlash;
    private bool isCueRunning = false;

    private bool resetRequested = false;


    IEnumerator TriggerSGD()
    {
        //Debug.Log("SGD TRIGGERED");
        //saccadeCue =
        //    GameObject.Find(
        //        "SGD_Cue");
        //Debug.Log("saccadeCue = " + saccadeCue);
        //isCueRunning = true;
        //isSGDWindow = true;

        //if (saccadeCue != null)
        //    saccadeCue.SetActive(true);
        //else
        //{
        //    Debug.Log("Nothing SGD_Cue");
        //}
        //yield return new WaitForSeconds(sgdDuration);

        //if (saccadeCue != null)
        //    saccadeCue.SetActive(false);

        //isSGDWindow = false;
        //isCueRunning = false;


        //if (saccadeCue == null)
        //{
        //    saccadeCue =
        //        GameObject.Find("SGD_Cue");

        //    Debug.Log(
        //        "Find Result = "
        //        + saccadeCue);
        //}

        //if (saccadeCue == null)
        //{
        //    Debug.LogError(
        //        "SGD_Cue not found!");

        //    yield break;
        //}

        //isCueRunning = true;

        //saccadeCue.SetActive(true);

        //yield return
        //    new WaitForSeconds(0.5f);

        //saccadeCue.SetActive(false);

        //isCueRunning = false;


        if (saccadeCue == null)
        {
            saccadeCue = GameObject.Find("SGD_Cue");
        }

        if (saccadeCue == null)
        {
            Debug.LogError("SGD_Cue not found!");
            yield break;
        }

        isCueRunning = true;
        lastSgdTime = Time.time;

        saccadeCue.SetActive(true);

        yield return new WaitForSeconds(sgdDuration);

        saccadeCue.SetActive(false);

        isCueRunning = false;




    }

    IEnumerator FlashLeft()
    {

        if (leftFlash == null)
        {
            leftFlash = GameObject.Find("LeftFlash");
        }

        if (leftFlash == null)
        {
            Debug.LogError("leftFlash not found!");
            yield break;
        }


        leftFlash.SetActive(true);

        yield return
            new WaitForSeconds(
                0.15f);

        leftFlash.SetActive(false);
    }

    IEnumerator FlashRight()
    {
        if (rightFlash == null)
        {
            rightFlash = GameObject.Find("RightFlash");
        }

        if (rightFlash == null)
        {
            Debug.LogError("rightFlash not found!");
            yield break;
        }

        rightFlash.SetActive(true);

        yield return
            new WaitForSeconds(
                0.15f);

        rightFlash.SetActive(false);
    }


    //void Awake()
    //{
    //    warningAudio =
    //        GetComponent<AudioSource>();

    //    Debug.Log(
    //        "Audio = "
    //        + warningAudio);
    //}



    private Vector2 currPosReal;
    public override void InjectRedirection()
    {

        if (Input.GetKeyDown(KeyCode.A))
        {
           
            warningAudio.Play();

            Debug.Log(
                "isPlaying = "
                + warningAudio.isPlaying);

            Debug.Log(
                "clip = "
                + warningAudio.clip);
        }


        if (warningAudio == null)
        {
            warningAudio =
                GetComponent<AudioSource>();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            warningAudio.Play();
            Debug.LogWarning("Play AudioSource");
        }

        currPosReal = Utilities.FlattenedPos2D(redirectionManager.currPosReal);

        var physicalSpaces = globalConfiguration.physicalSpaces;
        int userIndex = movementManager.physicalSpaceIndex;
        SingleSpace space = physicalSpaces[userIndex];

        Vector2 heatmapForce = GetAPFHeatmapForce(space, currPosReal);

        // 기존 APF force도 같이 쓰고 싶으면 추가
        Vector2 forceT = GetTotalForce(physicalSpaces, globalConfiguration.GetAvatarTransforms());
       

        //gravitation 추가 
        var userTransforms = globalConfiguration.GetAvatarTransforms();

        Vector2 gravitation =
            GetPrimarySteeringTargetDir(
                physicalSpaces,
                userTransforms)
            * 0.5f
            * forceT.magnitude;

        float obstacleInfluence = 3.5f;
        Vector2 totalForce = (forceT * obstacleInfluence + heatmapInfluence * heatmapForce).normalized;



        //얼마나 꺾어야 하는지 계산
        Vector2 currentForward =
            Utilities.FlattenedDir2D(
                redirectionManager.currDirReal);

        //float cross =currentForward.x * totalForce.y - currentForward.y * totalForce.x;

        //float turnNeed =
        //    Vector2.Angle(
        //        currentForward,
        //        totalForce)
        //    / 180f;

        //graviation 적용 버전으로 변경
        //Vector2 totalForce =
        //(
        //    forceT * obstacleInfluence
        //    + gravitation
        //    + heatmapInfluence * heatmapForce
        //).normalized;
        //Vector2 totalForce =
        //(
        //    forceT * obstacleInfluence
        //    +
        //    gravitation
        //    +
        //    heatmapInfluence * heatmapForce
        //).normalized;

        float cross = currentForward.x * totalForce.y - currentForward.y * totalForce.x;

        float turnNeed =
            Vector2.Angle(
                currentForward,
                totalForce)
                / 180f;

        bool crossedThreshold =
            prevTurnNeed < sgdTurnThreshold
            && turnNeed >= sgdTurnThreshold;

        bool cooldownReady =
            Time.time - lastSgdTime > sgdCooldown;

        if (crossedThreshold && cooldownReady && !isCueRunning)
        {
            StartCoroutine(TriggerSGD());
        }

        prevTurnNeed = turnNeed;



        //UpdateTotalForcePointer(totalForce);

        //큐 추가 
        //if (turnNeed >= resetTurnThreshold)
        //{
        //    //if (redirectionManager.resetter != null)
        //    //{
        //    //    redirectionManager.resetter.InitializeReset();
        //    //}

        //    //return;

        //    if (!redirectionManager.inReset)
        //    {
        //        redirectionManager.resetter.InitializeReset();
        //    }

        //    return;
        //}
        //    else
        //    {
        //        resetRequested = false;
        //    }
        
        

        if (turnNeed >= soundTurnThreshold)
        {
            if (!isCueRunning)
            {
                if (cross > 0)
                    StartCoroutine(FlashLeft());
                else
                    StartCoroutine(FlashRight());
            }

            if (warningAudio != null &&
                !warningAudio.isPlaying)
            {
                warningAudio.Play();
                Debug.LogWarning(
                    "Play Warning Sound");
            }
        }
        else if (turnNeed >= sgdTurnThreshold)
        {
            if (!isCueRunning)
            {
                if (cross > 0)
                    StartCoroutine(FlashLeft());
                else
                    StartCoroutine(FlashRight());
            }

            if (warningAudio != null &&
                warningAudio.isPlaying)
            {
                warningAudio.Stop();
            }
        }
        else
        {
            if (warningAudio != null && warningAudio.isPlaying)
                warningAudio.Stop();
        }


        //추가
        UpdateTotalForcePointer(totalForce);
        //Debug.Log(
        //    "HeatmapForce Magnitude = "
        //    + heatmapForce.magnitude);

        ApplyRedirectionByForce(totalForce, physicalSpaces);
    }


    Vector2 GetLookAheadSafeDirection(SingleSpace space, Vector2 userPos, Vector2 baseDir)
    {
        Vector2 bestDir = baseDir.normalized;
        float currentRisk = ComputeRiskAt(space, userPos);
        float bestScore = float.MaxValue;

        int samples = 9;
        float maxAngle = 90f;
        float lookAheadDist = gridSize * 4f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            float angle = Mathf.Lerp(-maxAngle, maxAngle, t);

            Vector2 dir =
                Quaternion.Euler(0, 0, angle) * baseDir.normalized;

            Vector2 testPos =
                userPos + dir * lookAheadDist;

            float futureRisk =
                ComputeRiskAt(space, testPos);

            float riskIncrease =
                Mathf.Max(0f, futureRisk - currentRisk);

            float score =
                futureRisk + 3f * riskIncrease;

            if (score < bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        return bestDir.normalized;
    }

    Vector2 GetAPFHeatmapForce(SingleSpace space, Vector2 userPos)
    {
        float eps = gridSize;

        float riskCenter = ComputeRiskAt(space, userPos);
        float riskXPlus = ComputeRiskAt(space, userPos + new Vector2(eps, 0));
        float riskXMinus = ComputeRiskAt(space, userPos - new Vector2(eps, 0));
        float riskYPlus = ComputeRiskAt(space, userPos + new Vector2(0, eps));
        float riskYMinus = ComputeRiskAt(space, userPos - new Vector2(0, eps));

        Vector2 gradient = new Vector2(
            (riskXPlus - riskXMinus) / (2f * eps),
            (riskYPlus - riskYMinus) / (2f * eps)
        );

        // 위험도가 증가하는 방향이 gradient
        // 사용자는 위험도가 낮아지는 방향으로 유도해야 하므로 -gradient
        Vector2 safeDir = -gradient;

        //장애물별  가중치 적용되도록 수정
        float riskMagnitude = gradient.magnitude;

        if (riskMagnitude < 0.0001f)
            return Vector2.zero;

        Vector2 lookAheadDir =
        GetLookAheadSafeDirection(
        space,
        userPos,
        safeDir);

        //return lookAheadDir * riskMagnitude;
        return
            safeDir.normalized
            *
            riskMagnitude;

        //return safeDir.normalized;
    }

    //float ComputeRiskAt(SingleSpace space, Vector2 pos)
    //{
    //    float risk = 0f;

    //    for (int obIndex = 0; obIndex < space.obstaclePolygons.Count; obIndex++)
    //    {
    //        var ob = space.obstaclePolygons[obIndex];

    //        float minDist = GetDistanceToPolygon(pos, ob);

    //        // Gaussian APF risk
    //        risk += riskWeight * Mathf.Exp(
    //            -(minDist * minDist) / (2f * riskSigma * riskSigma)
    //        );
    //    }

    //    return risk;
    //}

    //수정 전: heatmap에는 벽이 적용X
    //float ComputeRiskAt(
    //SingleSpace space,
    //Vector2 pos)
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

    //        // obstacle별 semantic risk 반영
    //        risk +=
    //            semanticWeight *
    //            Mathf.Exp(
    //                -(minDist * minDist)
    //                /
    //                (2f *
    //                 riskSigma *
    //                 riskSigma));
    //    }

    //    return risk;
    //}

    //수정 후: 벽도 heatmap에 적용해줌. 
    float ComputeRiskAt(
    SingleSpace space,
    Vector2 pos)
    {
        float risk = 0f;

        //----------------------------------
        // Obstacle risk
        //----------------------------------

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
                semanticWeight
                *
                Mathf.Exp(
                    -(minDist * minDist)
                    /
                    (
                        2f
                        *
                        riskSigma
                        *
                        riskSigma
                    )
                );
        }

        //----------------------------------
        // Wall risk
        //----------------------------------

        for (
            int i = 0;
            i <
            space.trackingSpace.Count;
            i++)
        {
            Vector2 a =
                space.trackingSpace[i];

            Vector2 b =
                space.trackingSpace[
                    (i + 1)
                    %
                    space.trackingSpace.Count];

            Vector2 nearest =
                GetNearestPointOnSegment(
                    pos,
                    a,
                    b);

            float wallDist =
                Vector2.Distance(
                    pos,
                    nearest);

            risk +=
                wallWeight
                *
                Mathf.Exp(
                    -(wallDist * wallDist)
                    /
                    (
                        2f
                        *
                        riskSigma
                        *
                        riskSigma
                    )
                );
        }

        return risk;
    }

    float GetDistanceToPolygon(Vector2 p, List<Vector2> polygon)
    {
        float minDist = float.MaxValue;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            Vector2 nearest = GetNearestPointOnSegment(p, a, b);
            float dist = Vector2.Distance(p, nearest);

            if (dist < minDist)
                minDist = dist;
        }

        return minDist;
    }

    Vector2 GetNearestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }
}