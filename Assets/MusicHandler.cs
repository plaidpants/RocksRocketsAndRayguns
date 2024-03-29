﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MusicHandler : NetworkBehaviour
{
    public int totalNumberOfRocksInLevel = 0;

    public AudioClip introMusicClip;
    public AudioClip[] musicClipLoops;
    public AudioClip outroMusicClip;

    AudioSource introMusic;
    AudioSource[] musicLoops;
    AudioSource outroMusic;

    public int lastMusicProgression = 1;
    public int musicProgression = 0;
    bool outroQueued = false;

    bool initialized = false;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        musicLoops = new AudioSource[musicClipLoops.Length];

        for (int i = 0; i < musicClipLoops.Length; i++)
        {
            //Add this audiosource as a component of the game object this script is attached to
            musicLoops[i] = gameObject.AddComponent<AudioSource>();
            //Assign the corresponding audio clip to its audio source
            musicLoops[i].clip = musicClipLoops[i];
            musicLoops[i].loop = false;
            musicLoops[i].volume = 0.5f;
        }

        // get the outro music
        outroMusic = gameObject.AddComponent<AudioSource>();
        outroMusic.clip = outroMusicClip;
        outroMusic.loop = false;
        outroMusic.volume = 0.5f;

        // start the intro music
        introMusic = gameObject.AddComponent<AudioSource>();
        introMusic.clip = introMusicClip;
        introMusic.loop = false;
        introMusic.volume = 0.5f;
        introMusic.Play();
        
        // queue the first loop playing and looping after the intro
        musicLoops[0].loop = true;
        musicLoops[0].PlayScheduled(AudioSettings.dspTime + introMusic.clip.length);

        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        // wait until we initialized, client start might be delayed
        if (!initialized)
            return;

        //Debug.Log("Rocks " + RockSphere.currentRocks + " total " + RockSphere.totalRocks + " destoyed " + RockSphere.destroyedRocks);

        // are we done shooting rocks and have not started the outro music
        if (RockSphere.destroyedRocks == totalNumberOfRocksInLevel)
        {
            RockSphere[] Rocks = FindObjectsOfType<RockSphere>();

            // double check that we are not spawning rocks
            if (Rocks.Length == 0)
            {
                //          Debug.Log("done destroying rocks");

                // check if we already have the outro music queue or we are still playing the intro
                if (!outroQueued)
                {
                    //              Debug.Log("outro is not playing or queued");

                    // is there a music progression queued
                    if (musicLoops[lastMusicProgression].isPlaying && musicLoops[musicProgression].isPlaying)
                    {
                        //                   Debug.Log("need to stop queued music before queing outro");

                        // stop the queued music from playing
                        musicLoops[musicProgression].Stop();

                        // stop the current music looping
                        musicLoops[lastMusicProgression].loop = false;

                        // stop the queued music from looping
                        musicLoops[musicProgression].loop = false;

                        // calculate how much of the music loop has played so far
                        double timeElapsed = (double)musicLoops[lastMusicProgression].timeSamples / musicLoops[lastMusicProgression].clip.frequency;

                        // calculate what's left to play of the music loop
                        double timeLeft = musicLoops[lastMusicProgression].clip.length - timeElapsed;

                        // don't loop the outro music
                        outroMusic.loop = false;

                        // Schedule the next music to play at the current time + the length time left in the current music
                        outroMusic.PlayScheduled(AudioSettings.dspTime + timeLeft);
                    }
                    else
                    {
                        //                   Debug.Log("queue outro");

                        // stop the current music looping
                        musicLoops[musicProgression].loop = false;

                        // calculate how much of the music loop has played so far
                        double timeElapsed = (double)musicLoops[musicProgression].timeSamples / musicLoops[musicProgression].clip.frequency;

                        // calculate what's left to play of the music loop
                        double timeLeft = musicLoops[musicProgression].clip.length - timeElapsed;

                        // don't loop the outro music
                        outroMusic.loop = false;

                        // Schedule the next music to play at the current time + the length time left in the current music
                        outroMusic.PlayScheduled(AudioSettings.dspTime + timeLeft);
                    }

                    outroQueued = true;
                }
            }
        }
        // is there a progression playing and nothing queued
        else if (musicLoops[musicProgression].isPlaying && !musicLoops[lastMusicProgression].isPlaying && !introMusic.isPlaying)
        {
            //Debug.Log("No music queued");

            // double check we are not spawning rocks
            if (RockSphere.destroyedRocks <= totalNumberOfRocksInLevel)
            {
                // do we need to advance the music based on the % of rocks destroyed?
                if (musicProgression < (int)((float)RockSphere.destroyedRocks / (float)totalNumberOfRocksInLevel * (float)musicClipLoops.Length))
                {
                    //                Debug.Log("Music progression " + musicProgression + " rocks " + totalNumberOfRocksInLevel + " destroyed " + RockSphere.destroyedRocks);

                    // turn off looping for the current music
                    musicLoops[musicProgression].loop = false;

                    // calculate how much of the music loop has played so far
                    double timeElapsed = (double)musicLoops[musicProgression].timeSamples / musicLoops[musicProgression].clip.frequency;

                    // calculate what's left to play of the music loop
                    double timeLeft = musicLoops[musicProgression].clip.length - timeElapsed;

                    // save last music being played so we can make sure it finishs and the new one starts before we queue another
                    lastMusicProgression = musicProgression;

                    // increment music progression
                    musicProgression++;

                    // turn on looping for the next music
                    musicLoops[musicProgression].loop = true;

                    // Schedule the next music to play at the current time + the length time left in the current music
                    musicLoops[musicProgression].PlayScheduled(AudioSettings.dspTime + timeLeft);
                }
            }
        }
    }
}
