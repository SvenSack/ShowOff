﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundMan : MonoBehaviour
{
    public static SoundMan instance = null;
    public AudioClip[] chainsaw = new AudioClip[4];
    public GameObject audioSource;
    public GameObject chainsawSoundObject;
    private AudioSource chainsawSoundSource;
    private Coroutine cueCutLoop;

    void Awake() {
        if(!instance )
            instance = this;
        else {
            Destroy(this.gameObject) ;
            return;
        }

        DontDestroyOnLoad(this.gameObject) ;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartCut()
    {
        chainsawSoundSource = GenerateAudio(chainsaw[0]);
        chainsawSoundObject = chainsawSoundSource.gameObject;
        chainsawSoundSource.Play();
        cueCutLoop = StartCoroutine(CueCutLoop(chainsawSoundSource.clip.length));
    }

    public void ToggleWood()
    {
        if(chainsawSoundSource.clip != chainsaw[3])
            SwapChainsawSound(chainsaw[3], true, true);
        else
            SwapChainsawSound(chainsaw[1], true, true);
    }

    public void StopCut()
    {
        if (chainsawSoundSource.clip != chainsaw[0])
            SwapChainsawSound(chainsaw[2], false, true);
        else
        {
            StopCoroutine(cueCutLoop);
            chainsawSoundSource.Stop();
        }
    }

    private AudioSource GenerateAudio(AudioClip audio)
    {
        GameObject inst = Instantiate(audioSource);
        AudioSource ret = inst.GetComponent<AudioSource>();
        ret.clip = audio;
        return ret;
    }

    private void SwapChainsawSound(AudioClip newSound, bool looping, bool spatial)
    {
        chainsawSoundSource = GenerateAudio(newSound);
        Destroy(chainsawSoundObject);
        chainsawSoundObject = chainsawSoundSource.gameObject;
        if (looping)
            chainsawSoundSource.loop = true;
        if (spatial)
            chainsawSoundSource.spatialBlend = .5f;
        chainsawSoundSource.Play();
    }

    IEnumerator CueCutLoop(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if(chainsawSoundSource.clip == chainsaw[0])
            SwapChainsawSound(chainsaw[1], true, true);
    }
}
