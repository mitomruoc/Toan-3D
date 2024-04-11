using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int loser;

    public Animator Teacher;
    public AnimationClip talk;
    public AudioClip emhay;
    public Animator Student;
    public AnimationClip wrong;
    public AnimationClip correct;

    private AudioSource _audioSource;
    public AudioClip WinAudioClip, LoseAudioClip;
    

    private void Awake()
    {
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
    }

    public void Start()
    {
        _audioSource.clip = emhay;
        _audioSource.Play();
        
        Teacher.Play(talk.name);
    }

    public void PlayAudioWin()
    {
        Teacher.Play(talk.name);
        Student.Play(correct.name);
        _audioSource.clip = WinAudioClip;
        _audioSource.Play();
    }
    
    public void PlayAudioLose()
    {
        Teacher.Play(talk.name);
        Student.Play(wrong.name);
        _audioSource.clip = LoseAudioClip;
        _audioSource.Play();
    }
}
