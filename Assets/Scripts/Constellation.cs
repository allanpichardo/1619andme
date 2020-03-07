using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Constellation: MonoBehaviour
{
    private static readonly int Color45Edb685 = Shader.PropertyToID("Color_45EDB685");
    public float speed = 1.0f;
    public float fadeTime = 20.0f;
    
    private Queue<Star> _path;
    private int _step;
    private int _max;
    private LineRenderer _lineRenderer;
    private CompletionListener _completionListener;
    private Star _current;
    private Star _next;
    
    public void SetPath(Queue<Star> stars)
    {
        _path = stars;
        _step = 0;
        _max = stars.Count;

        _lineRenderer = GetComponent<LineRenderer>();
        _step++;
        _lineRenderer.positionCount = _step;
        _current = stars.Dequeue();
        _lineRenderer.SetPosition(_step - 1, _current.gameObject.transform.position);
        
        ContinueSequence();
    }

    public void ContinueSequence()
    {
        _next = GetNext();
        _step++;
        StartCoroutine(DrawLineSegment());
    }

    private IEnumerator DrawLineSegment()
    {
        float colorInterp = ((_step - 1.0f) / _max);
        Debug.Log(colorInterp);
        Color lineColor = Color.Lerp(Color.white, Color.yellow, colorInterp);
        SetLineColor(lineColor);
        
        Vector3 start = _current.gameObject.transform.position;
        Vector3 end = _next.gameObject.transform.position;
        float step = 0.01f;
        float t = 0.0f;

        _lineRenderer.positionCount = _step;
        _lineRenderer.SetPosition(_step - 1, start);

        while (t < 1.0f)
        {
            _lineRenderer.SetPosition(_step - 1, Vector3.Lerp(start, end, t));
            t += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        _current = _next;
        
        if(IsFinished())
        {
            RevealStar(_current);
            _completionListener.OnFinished(this);
            BeginFadeSequence();
        }
    }

    private void BeginFadeSequence()
    {
        Color startColor = Color.yellow;
        Color endColor = Color.clear;
        StartCoroutine(FadeAnimation(startColor, endColor));
    }

    private IEnumerator FadeAnimation(Color startColor, Color endColor)
    {
        float elapsed = 0.0f;
        while (elapsed < fadeTime)
        {
            SetLineColor(Color.Lerp(startColor, endColor, elapsed / fadeTime));
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    private void RevealStar(Star current)
    {
        current.SetColor(Color.red);
        SetLineColor(Color.yellow);
    }
    
    private void SetLineColor(Color color)
    {
        _lineRenderer.material.SetColor(Color45Edb685, color);
    }

    private Star GetNext()
    {
        return _path.Dequeue();
    }

    public bool IsNext(Star star)
    {
        return star.GetAudioPoint().Equals(_next.GetAudioPoint());
    }

    public bool IsFinished()
    {
        return _path.Count == 0;
    }
    
    public void SetCompletionListener(CompletionListener listener)
    {
        _completionListener = listener;
    }
    
    public interface CompletionListener
    {
        void OnFinished(Constellation constellation);
    }
}
