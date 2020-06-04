﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CutMan : MonoBehaviour
{
    public GameObject currentCut;
    private Vector2 cutStart;
    private Vector2 cutUpdate;
    private bool isCutting;
    
    [SerializeField] GraphicRaycaster gRaycaster = null;
    [SerializeField] EventSystem eventSystem = null;
    [SerializeField] float marginOfError = 0.1f;
    private int trunkMask;

    [SerializeField] GameObject cutParticleObject = null;
    [SerializeField] GameObject fallParticleObject = null;
    private GameObject cutParticleInstance;

    private CuttableTreeScript[] trees;
    public int[] treeHps;
    private CutTarget[] cutTargets;
    [SerializeField] GameObject cutTargetPrefab = null;
    [SerializeField] private float cutTargetDistance = 5f;
    
    private SoundMan soundMan;
    private UIMan uiMan;

    [SerializeField] float[] leafScaleValues = new []{1f,1f,1f,1f,1f,1f};
    public Sprite[] leafParticles;

    [SerializeField] float cutForce = 100f;

    public float forgivingness = 1f;
    private List<Coroutine> bagOfCutStops = new List<Coroutine>();
    private bool cutFailing;
    
    public bool isInCombo;
    public int comboCount = 0;
    [SerializeField] private TextMeshProUGUI comboText;
    
    
    private void Awake()
    {
        soundMan = FindObjectOfType<SoundMan>();
        uiMan = FindObjectOfType<UIMan>();
        trunkMask = LayerMask.GetMask("Trunks");
        trees = FindObjectsOfType<CuttableTreeScript>();
        treeHps = new int[trees.Length];
        cutTargets = new CutTarget[trees.Length];
        for (int i = 0; i < treeHps.Length; i++)
        {
            treeHps[i] = Random.Range(1, 3);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            List<RaycastResult> castHits = new List<RaycastResult>();
            PointerEventData eventPoint = new PointerEventData(eventSystem) {position = Input.mousePosition};
            gRaycaster.Raycast(eventPoint, castHits);
            if (castHits.Count > 0)
            {
                foreach (var t in castHits)
                {
                    if (t.gameObject.GetComponentInChildren<CutTarget>() != null)
                    {
                        StartCut(t.gameObject);
                    }
                }
            }
        }

        if (isInCombo)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Stopped cutting because click lift");
                StopCut();
                foreach (var routine in bagOfCutStops)
                {
                   StopCoroutine(routine); 
                }
            }
            else if(Input.GetMouseButton(0))
            {
                List<RaycastResult> castHits = new List<RaycastResult>();
                PointerEventData eventPoint = new PointerEventData(eventSystem) {position = Input.mousePosition};
                gRaycaster.Raycast(eventPoint, castHits);
                bool hit = false;
                if (castHits.Count > 0)
                {
                    foreach (var t in castHits)
                    {
                        CutTarget targ = t.gameObject.GetComponentInChildren<CutTarget>();
                        if (targ != null)
                        {
                            // Debug.Log("Cut continues");
                            GameObject targO = targ.transform.parent.gameObject;
                            if (targO != currentCut && StartCut(targO))
                            {
                                currentCut = targO;
                                cutFailing = false;
                            }
                            hit = true;
                        }
                    }
                }
                if (!hit && !cutFailing)
                {
                    Debug.Log("Stopped cutting because click left");
                    Coroutine rout = StartCoroutine(InitiateStopCut());
                    bagOfCutStops.Add(rout);
                }
            
            }
        }
        

        if (isInCombo)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100000, trunkMask) && currentCut != null)
            {
                if (hit.collider.gameObject.GetComponent<CuttableTreeScript>() ==
                    currentCut.GetComponentInChildren<CutTarget>().target)
                {
                    // Debug.DrawRay (ray.origin, ray.direction * 50000000, Color.green, 100f);
                    if (!isCutting && CheckCutSpot(Input.mousePosition))
                    {
                        // Debug.Log("hit tree");
                        isCutting = true;
                        soundMan.ToggleWood();
                        cutParticleInstance = Instantiate(cutParticleObject);
                        if(currentCut.GetComponentInChildren<CutTarget>().goesLeft)
                            cutParticleInstance.transform.rotation = Quaternion.Euler(0,0,180);
                        foreach (var part in cutParticleInstance.GetComponentsInChildren<ParticleSystem>())
                        {
                            part.Play();
                        }
                        cutParticleInstance.transform.position = hit.point;
                        soundMan.chainsawSoundObject.transform.position = hit.point;
                        cutStart = Input.mousePosition;
                    }
                    if(isCutting)
                    {
                        // Debug.Log("still hitting tree");
                        cutUpdate = Input.mousePosition;
                        cutParticleInstance.transform.position = hit.point;
                        soundMan.chainsawSoundObject.transform.position = hit.point;
                    }
                }
            }
            else
            {
                // Debug.DrawRay (ray.origin, ray.direction * 50000000, Color.red, 100f);
                if (isCutting)
                {
                    float dist = Vector2.Distance(cutStart, Input.mousePosition);
                    // Debug.Log("stopped hitting tree at distance " + dist);
                    if (Mathf.Abs(dist) > marginOfError && CheckCutSpotCut(cutStart, Input.mousePosition))
                    {
                        GameObject cutPlane = InitiateCut(cutStart, cutUpdate);
                        CuttableTreeScript target = currentCut.GetComponentInChildren<CutTarget>().target;
                        GameObject newTreePiece = target.CutAt(cutPlane.transform.position, cutPlane.transform.up, cutForce);
                        Destroy(cutPlane);
                        FellTree(newTreePiece, target); // this does the visual and auditory stuff for the tree falling
                        for (int i = 0; i < trees.Length; i++)
                        {
                            if (trees[i] == target)
                            {
                                treeHps[i]--;
                                if(treeHps[i] == 0)
                                    uiMan.IncreaseScore(true);
                            }
                        }
                        Destroy(currentCut);
                        ComboCut();
                        Coroutine rout = StartCoroutine(InitiateStopCut());
                        bagOfCutStops.Add(rout);
                        isCutting = false;
                    }
                    foreach (var part in cutParticleInstance.GetComponents<ParticleSystem>())
                    {
                        part.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
                else
                {
                    if(cutParticleInstance != null)
                    {
                        float zValue = cutParticleInstance.transform.position.z;
                        Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
                        cutParticleInstance.transform.position = ray1.GetPoint(zValue);
                    }
                }
            }
        }
        
        PlaceCutSpots();
    }

    private GameObject InitiateCut(Vector2 start, Vector2 finish)
    {
        Ray ray = Camera.main.ScreenPointToRay(start);
        Physics.Raycast(ray, out var hit);
        Vector3 startPoint = hit.point;
        
        Ray ray2 = Camera.main.ScreenPointToRay(finish);
        Physics.Raycast(ray2, out hit);
        Vector3 finishPoint = hit.point;
        
        Vector3 targetLocation = Vector3.Lerp(startPoint, finishPoint, 0.5f);
        float targetZRotation = Vector3.Angle(startPoint, finishPoint);
        Vector3 targetRotation = Vector3.zero;
        targetRotation.z = targetZRotation;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = targetLocation;
        plane.transform.Rotate(targetRotation);
        return plane;
    }

    private Vector3 GetMouseWorld()
    {
        Camera main;
        Ray ray = (main = Camera.main).ScreenPointToRay(Input.mousePosition);
        float dist = Vector3.Distance(main.transform.position,
            currentCut.GetComponentInChildren<CutTarget>().target.transform.position);
        // Debug.DrawRay(ray.origin, ray.direction * dist, Color.yellow, 10f);
        return ray.GetPoint(dist);
    }

    private bool CheckCutSpotCut(Vector2 pos1, Vector2 pos2)
    {
        List<RaycastResult> castHits1 = new List<RaycastResult>();
        List<RaycastResult> castHits2 = new List<RaycastResult>();
        PointerEventData eventPoint1 = new PointerEventData(eventSystem) {position = pos1};
        PointerEventData eventPoint2 = new PointerEventData(eventSystem) {position = pos2};
        gRaycaster.Raycast(eventPoint1, castHits1);
        gRaycaster.Raycast(eventPoint2, castHits2);
        List<GameObject> targ1 = new List<GameObject>();
        List<GameObject> targ = new List<GameObject>();
        foreach (var hit in castHits1)
        {
            targ1.Add(hit.gameObject);
        }

        foreach (var check in castHits2)
        {
            if (targ1.Contains(check.gameObject))
            {
                targ.Add(check.gameObject);
            }
        }
        return targ.Contains(currentCut);
    }
    
    private bool CheckCutSpot(Vector2 pos)
    {
        List<RaycastResult> castHits = new List<RaycastResult>();
        PointerEventData eventPoint = new PointerEventData(eventSystem) {position = pos};
        gRaycaster.Raycast(eventPoint, castHits);
        List<GameObject> targ = new List<GameObject>();
        foreach (var hit in castHits)
        {
            targ.Add(hit.gameObject);
        }
        return targ.Contains(currentCut);
    }

    private void PlaceCutSpots()
    {
        for (int i = 0; i < cutTargets.Length; i++)
        {
            if (cutTargets[i] == null && treeHps[i] > 0)
            {
                if (Vector3.Distance(trees[i].transform.position, Camera.main.transform.position) < cutTargetDistance)
                {
                    cutTargets[i] = Instantiate(cutTargetPrefab, gRaycaster.transform).GetComponentInChildren<CutTarget>();
                    cutTargets[i].target = trees[i];
                    cutTargets[i].goesLeft = Random.Range(0, 2) == 0;
                    cutTargets[i].setLifeTime = forgivingness * 3;
                    Transform tempTrans = cutTargets[i].transform.parent;
                    BoxCollider[] boxes = trees[i].transform.parent.GetComponentsInChildren<BoxCollider>();
                    Vector3 targetPosition = Vector3.zero;
                    switch (treeHps[i])
                    {
                        case 3:
                            Vector3 roof = boxes[2].transform.position + new Vector3(0, .5f * boxes[2].transform.localScale.y, 0);
                            Vector3 floor = boxes[2].transform.position - new Vector3(0, .5f * boxes[2].transform.localScale.y, 0);
                            targetPosition = boxes[2].transform.position;
                            Debug.DrawLine(roof,floor,Color.red,100f);
                            targetPosition.y = Random.Range(roof.y, floor.y);
                            break;
                        case 2:
                            Vector3 roof1 = boxes[1].transform.position + new Vector3(0, .5f * boxes[1].transform.localScale.y, 0);
                            Vector3 floor1 = boxes[1].transform.position - new Vector3(0, .5f * boxes[1].transform.localScale.y, 0);
                            Debug.DrawLine(roof1,floor1,Color.red,100f);
                            targetPosition = boxes[1].transform.position;
                                                targetPosition.y = Random.Range(roof1.y, floor1.y);
                            break;
                        case 1:
                            Vector3 roof2 = boxes[0].transform.position + new Vector3(0, .5f * boxes[0].transform.localScale.y, 0);
                            Vector3 floor2 = boxes[0].transform.position - new Vector3(0, .5f * boxes[0].transform.localScale.y, 0);
                            targetPosition = boxes[0].transform.position;
                            Debug.DrawLine(roof2,floor2,Color.red,100f);
                            targetPosition.y = Random.Range(roof2.y, floor2.y);
                            break;
                    }
                    tempTrans.position = Camera.main.WorldToScreenPoint(targetPosition);
                    float offSet = Random.Range(-20f, 20f);
                    tempTrans.rotation = Quaternion.Euler(0,0, offSet);
                    if (!cutTargets[i].goesLeft)
                        tempTrans.GetChild(0).rotation = Quaternion.Euler(0,0,180+offSet);
                }
            }
        }
    }

    private bool StartCut(GameObject target)
    {
        RectTransform rec = target.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rec.GetWorldCorners(corners);
        float width = Vector3.Distance(Vector3.Lerp(corners[2],corners[3],0.5f),Vector3.Lerp(corners[0],corners[1],0.5f));
        switch (target.GetComponentInChildren<CutTarget>().goesLeft)
        { 
            case true:
                float dist = Vector2.Distance(Vector3.Lerp(corners[2],corners[3],0.5f),
                    Input.mousePosition);
                if (dist < width / 3)
                {
                    soundMan.StartCut();
                    currentCut = target;
                    soundMan.chainsawSoundObject.transform.position = GetMouseWorld();
                    isInCombo = true;
                    // Debug.DrawLine(Vector3.Lerp(corners[2],corners[3],0.5f), Input.mousePosition,Color.cyan,1000);
                    return true;
                }
                break;
            case false:
                float dist1 = Vector2.Distance(Vector3.Lerp(corners[0],corners[1],0.5f),
                    Input.mousePosition);
                if (dist1 < width / 3)
                {
                    soundMan.StartCut();
                    currentCut = target;
                    soundMan.chainsawSoundObject.transform.position = GetMouseWorld();
                    isInCombo = true;
                    // Debug.DrawLine(Vector3.Lerp(corners[0],corners[1],0.5f), Input.mousePosition, Color.cyan,1000);
                    return true;
                }
                break;
        }

        return false;
    }

    private void StopCut()
    {
        soundMan.StopCut();
        if(cutParticleInstance != null)
            foreach (var part in cutParticleInstance.GetComponentsInChildren<ParticleSystem>())
            {
                part.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        currentCut = null;
        isCutting = false;
        isInCombo = false;
        comboCount = 0;
        comboText.fontSize = 0;
    }

    IEnumerator InitiateStopCut()
    {
        cutFailing = true;
        yield return new WaitForSeconds(forgivingness);
        if (cutFailing)
        {
            StopCut();
        }
    }

    private void FellTree(GameObject newTreePiece, CuttableTreeScript target)
    {
        GameObject newPartI = Instantiate(fallParticleObject, newTreePiece.transform);
        ParticleSystem newPart = newPartI.transform.GetChild(0).GetComponent<ParticleSystem>();
        int newPartIndex = target.leafParticleIndex;
        newPart.textureSheetAnimation.SetSprite(0,leafParticles[newPartIndex]);
        if (leafScaleValues[newPartIndex] != 1)
        {
            var newPartMain = newPart.main;
            newPartMain.startSizeMultiplier = leafScaleValues[newPartIndex];
        }
        var newPartShape = newPart.shape;
        newPartShape.mesh = newTreePiece.GetComponent<MeshFilter>().mesh;
        newPart.Play();
        newTreePiece.GetComponent<TreeFallParticle>().fallSound = soundMan.TreeFall(newTreePiece);
    }

    private void ComboCut()
    {
        comboCount++;
        if (comboText.fontSize == 0 && comboCount > 1)
            comboText.fontSize = 36;
        string temp = comboText.text;
        temp = temp.Replace((comboCount - 1) + "x", comboCount + "x");
        comboText.text = temp;
    }
}
