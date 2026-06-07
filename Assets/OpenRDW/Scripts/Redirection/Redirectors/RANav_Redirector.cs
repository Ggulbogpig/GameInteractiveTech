using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RANav_Redirector : DynamicAPF_Redirector
{
    public float gridSize = 0.25f;
    public float riskSigma = 0.8f;
    //public float riskWeight = 5.0f;
    public float heatmapInfluence = 3.0f;
    public List<float> obstacleWeights =
    new List<float>()
{
    30.0f,
    50.0f,
    80.0f,
    100.0f
};
    public Color[] obstacleColors =
    {
    Color.green,
    Color.yellow,
    new Color(1f,0.5f,0f),
    Color.red
};

    public float lambdaRisk = 2.0f; //risk penalty
    public float predictionDistance = 2.0f; //얼마나 앞을 planning할지

    public float gLow = 30f;
    public float gMid = 60f;
    public float gHigh = 100f;

    public enum RiskLevel
    {
        Low,
        Mid,
        High,
        MaxHigh
    }

    public AudioSource warningAudio;

    public bool useSGD = false;
    public bool useSound = false;

    RiskLevel EvaluateRisk(float risk)
    {
        if (risk < gLow)
            return RiskLevel.Low;

        if (risk < gMid)
            return RiskLevel.Mid;

        if (risk < gHigh)
            return RiskLevel.High;

        return RiskLevel.MaxHigh;
    }



    private Vector2 currPosReal;

    Vector2 GetFutureGoal(
    List<SingleSpace> spaces,
    List<Transform> users)
    {
        Vector2 dir =
            GetPrimarySteeringTargetDir(
                spaces,
                users);

        return
            currPosReal
            +
            dir.normalized
            *
            predictionDistance;
    }

    Vector2 GetRiskAwareDirection(
    SingleSpace space,
    Vector2 start,
    Vector2 goal)
    {
        Vector2 bestDir =
            (goal - start).normalized;

        float bestCost =
            float.MaxValue;

        int samples = 16;

        for (int i = 0; i < samples; i++)
        {
            float angle =
                i
                *
                360f
                /
                samples;

            Vector2 dir =
                Quaternion
                .Euler(
                    0,
                    0,
                    angle)
                *
                Vector2.right;

            Vector2 testPos =
                start
                +
                dir
                *
                gridSize
                *
                3f;

            float g =
                Vector2.Distance(
                    start,
                    testPos);

            float h =
                Vector2.Distance(
                    testPos,
                    goal);

            float risk =
                ComputeRiskAt(
                    space,
                    testPos);

            float cost =
                g
                +
                h
                +
                lambdaRisk
                *
                risk;

            if (cost < bestCost)
            {
                bestCost =
                    cost;

                bestDir =
                    dir;
            }
        }

        return bestDir;
    }

    public override void InjectRedirection()
    {
        currPosReal =
            Utilities.FlattenedPos2D(
                redirectionManager.currPosReal);

        var physicalSpaces =
            globalConfiguration.physicalSpaces;

        var userTransforms =
            globalConfiguration
            .GetAvatarTransforms();

        int userIndex =
            movementManager
            .physicalSpaceIndex;

        SingleSpace space =
            physicalSpaces[userIndex];

        Vector2 futureGoal =
            GetFutureGoal(
                physicalSpaces,
                userTransforms);

        Vector2 heatmapForce =
            GetRiskAwareDirection(
                space,
                currPosReal,
                futureGoal);

        Vector2 forceT =
            GetTotalForce(
                physicalSpaces,
                userTransforms);

        float obstacleInfluence = 3.5f;

        Vector2 gravitation =
            GetPrimarySteeringTargetDir(
                physicalSpaces,
                userTransforms)
            *
            0.5f
            *
            forceT.magnitude;

        //----------------------------------
        // Risk policy
        //----------------------------------

        float currRisk =
            ComputeRiskAt(
                space,
                currPosReal);

        RiskLevel level =
            EvaluateRisk(
                currRisk);

        float gainMultiplier = 1f;


        // 기본 OFF
        useSGD = false;
        useSound = false;

        switch (level)
        {
            case RiskLevel.Low:

                // 기존 RDW만
                break;

            case RiskLevel.Mid:

                // SGD 추가
                useSGD = true;
                break;

            case RiskLevel.High:

                // SGD + Sound
                useSGD = true;
                useSound = true;
                break;

            case RiskLevel.MaxHigh:

                if (
                    redirectionManager
                    .resetter
                    !=
                    null)
                {
                    redirectionManager
                        .resetter
                        .InitializeReset();
                }

                return;
        }

        //----------------------------------
        // Final force
        //----------------------------------

        Vector2 totalForce =
        (
            forceT
            *
            obstacleInfluence
            +
            gravitation
            +
            heatmapInfluence
            *
            heatmapForce
            *
            gainMultiplier
        ).normalized;

        UpdateTotalForcePointer(
            totalForce);

        //--------------------------------
        // SGD modulation
        //--------------------------------

        //if (useSGD)
        //{
        //    // Blink / Saccade 순간
        //    if (
        //        Random.value
        //        <
        //        0.02f)
        //    {
        //        totalForce =
        //            (
        //                totalForce
        //                *
        //                1.8f
        //            ).normalized;
        //    }
        //}

        //--------------------------------
        // Sound cue
        //--------------------------------

        if (useSound)
        {
            if (
                warningAudio
                &&
                !warningAudio.isPlaying)
            {
                warningAudio.Play();
            }
        }
        else
        {
            if (
                warningAudio
                &&
                warningAudio.isPlaying)
            {
                warningAudio.Stop();
            }
        }

        ApplyRedirectionByForce(
            totalForce,
            physicalSpaces);
    }

    //resetter 적용 전
    //public override void InjectRedirection()
    //{
    //    currPosReal = Utilities.FlattenedPos2D(redirectionManager.currPosReal);

    //    var physicalSpaces = globalConfiguration.physicalSpaces;

    //    // 추가
    //    var userTransforms =
    //        globalConfiguration.GetAvatarTransforms();

    //    int userIndex = movementManager.physicalSpaceIndex;
    //    SingleSpace space = physicalSpaces[userIndex];

    //    //Vector2 heatmapForce = GetAPFHeatmapForce(space, currPosReal);
    //    Vector2 futureGoal =
    //        GetFutureGoal(
    //            physicalSpaces,
    //            userTransforms);

    //    Vector2 heatmapForce =
    //        GetRiskAwareDirection(
    //            space,
    //            currPosReal,
    //            futureGoal);
    //    // 기존 APF force도 같이 쓰고 싶으면 추가
    //    Vector2 forceT = GetTotalForce(physicalSpaces, userTransforms);

    //    float obstacleInfluence = 3.5f;

    //    // DynamicAPF의 gravitation 복구
    //    Vector2 gravitation =
    //        GetPrimarySteeringTargetDir(
    //            physicalSpaces,
    //            userTransforms)
    //        *
    //        0.5f
    //        *
    //        forceT.magnitude;

    //    Vector2 totalForce =
    //    (
    //        forceT * obstacleInfluence
    //        +
    //        gravitation
    //        +
    //        heatmapInfluence
    //        *
    //        heatmapForce
    //    ).normalized;

    //    UpdateTotalForcePointer(totalForce);
    //    ApplyRedirectionByForce(totalForce, physicalSpaces);
    //}

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
    float ComputeRiskAt(
    SingleSpace space,
    Vector2 pos)
    {
        float risk = 0f;

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
                obstacleWeights
                [obIndex];

            // obstacle별 semantic risk 반영
            risk +=
                semanticWeight *
                Mathf.Exp(
                    -(minDist * minDist)
                    /
                    (2f *
                     riskSigma *
                     riskSigma));
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
