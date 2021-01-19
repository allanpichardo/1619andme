using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OculusSampleFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Star : MonoBehaviour
{
    private AudioPoint _audioPoint;
    private SkyGenerator _skyGenerator;
    private Animator _animator;
    
    public float timeToActivation = 1.0f;
    
    private bool _isActivating = false;
    private bool _hasPlayed = false;
    private static readonly int Color45Edb685 = Shader.PropertyToID("Color_45EDB685");
    private static readonly int IsSelected = Animator.StringToHash("isSelected");

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void OnPointerEnter()
    {
        _animator.SetBool(IsSelected, true);
    }

    public void OnPointerExit()
    {
        _animator.SetBool(IsSelected, false);
        OnLookExit();
    }

    public void OnActivate()
    {
        if (!_hasPlayed)
        {
            _hasPlayed = true;
            OnLookedEnter();
        }
    }

    public void HandleInteraction(InteractableStateArgs state)
    {
        switch (state.NewInteractableState)
        {
            case InteractableState.ProximityState:
            case InteractableState.ContactState:
                OnPointerEnter();
                break;
            case InteractableState.ActionState:
                OnActivate();
                break;
            default:
                OnPointerExit();
                break;
        }
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
        if (audioPoint.IsInAfrica())
        {
            GetComponent<ButtonController>().enabled = false;
        }
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

    public override bool Equals(object obj)
    {
        if ((obj == null) || this.GetType() != obj.GetType()) 
        {
            return false;
        }

        AudioPoint p = ((Star) obj).GetAudioPoint(); 
        return p.id == _audioPoint.id;
    }
}
