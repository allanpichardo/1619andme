using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour
{
    private AudioPoint _audioPoint;
    private SkyGenerator _skyGenerator;

    public float timeToActivation = 1.0f;
    private bool _isActivating = false;

    private static readonly int Color45Edb685 = Shader.PropertyToID("Color_45EDB685");

    public void SetSkyGenerator(SkyGenerator skyGenerator)
    {
        _skyGenerator = skyGenerator;
    }

    public void OnLookedEnter()
    {
        _isActivating = true;
        StartCoroutine(ActivationTimer());
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
