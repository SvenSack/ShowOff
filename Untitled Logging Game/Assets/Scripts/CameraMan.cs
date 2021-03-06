﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Animal;
using UnityEngine;
using UnityEngine.Profiling;

public class CameraMan : MonoBehaviour
{
    public Transform[] zoomLocations;
    [SerializeField] private List<int> treesToCut0;
    [SerializeField] private List<int> treesToCut1;
    [SerializeField] private List<int> treesToCut2;
    [SerializeField] private List<int> treesToCut3;
    public List<int>[] treesToCutEach;
    [SerializeField] private List<Transform> treesToPlant0;
    [SerializeField] private List<Transform> treesToPlant1;
    [SerializeField] private List<Transform> treesToPlant2;
    [SerializeField] private List<Transform> treesToPlant3;
    public List<Transform>[] treesToPlantEach;
    [SerializeField] private List<AnimalSprite> animalsForRegion0;
    [SerializeField] private List<AnimalSprite> animalsForRegion1;
    [SerializeField] private List<AnimalSprite> animalsForRegion2;
    [SerializeField] private List<AnimalSprite> animalsForRegion3;
    public List<AnimalSprite>[] animalsToMeetEach;
    public int currentLocation = -1;
    public Camera mainCam;
    public float zoomTime = 5f;
    private CutMan cutMan;
    private PlantMan plantMan;
    private GameObject mapButton;

    void Start()
    {
        Profiler.SetAreaEnabled(ProfilerArea.UIDetails, false);
        Profiler.SetAreaEnabled(ProfilerArea.UI, false);
        mainCam = Camera.main;
        cutMan = FindObjectOfType<CutMan>();
        plantMan = FindObjectOfType<PlantMan>();
        treesToCutEach = new[] {treesToCut0, treesToCut1, treesToCut2, treesToCut3};
        treesToPlantEach = new[] {treesToPlant0, treesToPlant1, treesToPlant2, treesToPlant3};
        animalsToMeetEach = new[] {animalsForRegion0, animalsForRegion1, animalsForRegion2, animalsForRegion3};
        mapButton = GameObject.FindGameObjectWithTag("MapButton");
        mapButton.SetActive(false);
    }

    public void MoveOn()
    {
        currentLocation++;
        if(currentLocation < zoomLocations.Length)
        {
            FindObjectOfType<UIMan>().allowMovement = false;
            mainCam.transform.LeanMove(zoomLocations[currentLocation].position, zoomTime);
            mainCam.transform.LeanRotate(zoomLocations[currentLocation].rotation.eulerAngles, zoomTime);
            StartCoroutine(SetTargets(zoomTime));
        }
        else
        {
            mapButton.SetActive(true);
            mapButton.GetComponent<MapButtonGlow>().StartGlow();
        }
    }

    IEnumerator SetTargets(float delay)
    {
        yield return new WaitForSeconds(delay);
        cutMan.currentTargetIndices = treesToCutEach[currentLocation];
        plantMan.currentTreeSpots = plantMan.currentTreeSpots.Union(treesToPlantEach[currentLocation]).ToList<Transform>();
        foreach (var animal in animalsToMeetEach[currentLocation])
        {
            animal.SnapToCamera(true);
        }
        FindObjectOfType<UIMan>().allowMovement = true;
    }
}
