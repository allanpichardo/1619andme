using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Star : MonoBehaviour
{
    private AudioPoint _audioPoint;
    private SkyGenerator _skyGenerator;
    private AudioSource _audioSource;

    public float timeToActivation = 1.0f;
    private bool _isActivating = false;

    private static readonly int Color45Edb685 = Shader.PropertyToID("Color_45EDB685");

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void SetSkyGenerator(SkyGenerator skyGenerator)
    {
        _skyGenerator = skyGenerator;
    }

    public void OnLookedEnter()
    {
        _isActivating = true;
        StartCoroutine(ActivationTimer());
    }

    IEnumerator PlayAudio()
    {
        string url = Path.Combine(Application.streamingAssetsPath, "sounds", $"{_audioPoint.id}.mp3");
        
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.Send();

            if (www.isError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                _audioSource.PlayOneShot(audioClip);
            }
        }
    }

    public void Play()
    {
        if (!_audioSource.isPlaying)
        {
            StartCoroutine(PlayAudio());
        }
    }

    public void Stop()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private IEnumerator ActivationTimer()
    {
        float elapsed = 0f;
        while (elapsed < timeToActivation)
        {
            if (_isActivating)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            else
            {
                break;
            }
        }

        if (_isActivating)
        {
            _skyGenerator.OnLookAtStar(this);
            _isActivating = false;
        }
    }

    public void OnLookExit()
    {
        _isActivating = false;
    }

    public void SetAudioPoint(AudioPoint audioPoint)
    {
        this._audioPoint = audioPoint;
    }

    public void SetColor(Color color)
    {
        this.GetComponent<Renderer>().material.SetColor(Color45Edb685, color);
    }

    public AudioPoint GetAudioPoint()
    {
        return this._audioPoint;
    }

    public void PrintInfo()
    {
        Debug.Log(_audioPoint.ToString());
    }
    
}
