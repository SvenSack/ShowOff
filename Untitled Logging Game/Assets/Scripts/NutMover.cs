﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NutMover : MonoBehaviour
{
    public float speed;
    public int treeIndex;
    public float floorHeight;
    private float fade = 1;
    private Image nut;
    private UIMan uiMan;
    private PlantMan plantMan;
    public AudioSource nutDrop;
    private bool nutLanded;
    
    // Start is called before the first frame update
    void Start()
    {
        nut = GetComponent<Image>();
        uiMan = FindObjectOfType<UIMan>();
        transform.LeanRotate(new Vector3(0, 0, Random.Range(-3,2)*360f+(Random.Range(0,2)*2-1)*90), (transform.position.y - floorHeight) / speed);
        plantMan = FindObjectOfType<PlantMan>();
    }

    // Update is called once per frame
    void Update()
    {
        var nutPosition = transform.position;
        if (nutPosition.y > floorHeight)
        {
            transform.position += new Vector3(0,-speed*Time.deltaTime,0);
            // Debug.Log(nutPosition.y);
            if (nutPosition.y < floorHeight)
            {
                nutPosition.y = floorHeight;
            }
        }
        else
        {
            if (!nutLanded)
            {
                nutLanded = true;
                nutDrop.Play();
                Debug.Log("nut goes tok");
            }
            if (fade > 0)
            {
                fade -= 1 * Time.deltaTime;
                nut.color = new Color(1,1,1,fade);
            }
            else
            {
                uiMan.seedCombo = 0;
                uiMan.seedComboText.fontSize = 0;
                Destroy(gameObject);
            }
        }
    }

    public void GrabNut()
    {
        plantMan.plantSeed(treeIndex);
        uiMan.IncreaseScore(false);
        Destroy(gameObject);
    }
}
