using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Constellation: MonoBehaviour
{
    public float speed = 1.0f;
    
    private Queue<Star> _path;
    private int _step;
    private int _count;
    private LineRenderer _lineRenderer;
    private CompletionListener _completionListener;
    private Star _current;
    private Star _next;
    
    public void SetPath(Queue<Star> stars)
    {
        _path = stars;
        _step = 0;
        _count = stars.Count;

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
        Debug.Log($"Continuing to {_next.GetAudioPoint()}");
        _step++;
        StartCoroutine(DrawLineSegment());
    }

    private IEnumerator DrawLineSegment()
    {
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

        if (!IsFinished())
        {
            _current = _next;
        }
        else
        {
            _completionListener.OnFinished(this);
        }
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
