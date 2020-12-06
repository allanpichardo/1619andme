using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HandController : MonoBehaviour
{
    public Transform trackingSpace;
    public float raycastDistance = 10000.0f;
    public bool debugRay = true;
    
    private OVRHand _hand;
    private Star _lastStar;
    private LineRenderer _lineRenderer;

    private void Start()
    {
        _hand = GetComponent<OVRHand>();
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material.color = Color.yellow;
        _lineRenderer.startWidth = 0.01f;
        _lineRenderer.endWidth = 0.01f;
        _lineRenderer.enabled = debugRay;
    }

    private void Update()
    {
        
        if (_hand.IsPointerPoseValid && _hand.IsDataHighConfidence)
        {
            Vector3 handPos = trackingSpace.TransformPoint(_hand.PointerPose.transform.position);
            Vector3 handForward = trackingSpace.TransformDirection(_hand.PointerPose.transform.forward);
            Vector3 remotePos = handPos + (handForward * raycastDistance);
            
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPositions(new []{handPos, remotePos}); //todo: get rid of renderer or set flag
            
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(handPos, handForward, out hit, raycastDistance))
            {
                Star star = hit.collider.gameObject.GetComponent<Star>();
                if (star != null)
                {
                    if (_lastStar != null && !_lastStar.Equals(star))
                    {
                        _lastStar.OnPointerExit();
                    }
                    star.OnPointerEnter();

                    if (_hand.GetFingerIsPinching(OVRHand.HandFinger.Index) && _hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.9f)
                    {
                        star.OnActivate();
                    }

                    _lastStar = star;
                }
            }
            else
            {
                if (_lastStar != null)
                {
                    _lastStar.OnPointerExit();
                    _lastStar = null;
                }
            }
        }
        else
        {
            if (_lastStar != null)
            {
                _lastStar.OnPointerExit();
                _lastStar = null;
            }
        }
    }
}
