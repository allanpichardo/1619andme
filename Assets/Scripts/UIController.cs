using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    private Camera _camera;

    public Text hintText;

    public Text journeyText;
    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out hit, 105.0f))
        {
            StartCoroutine(FadeText(hintText, Color.yellow));
        }
        else
        {
            StartCoroutine(FadeText(hintText, Color.clear));
        }
    }

    private IEnumerator FadeText(Text text, Color targetColor, float speed = 1.0f)
    {
        Color startColor = text.color;
        float t = 0.0f;
        while (t < 1.0f)
        {
            text.color = Color.Lerp(startColor, targetColor, t);
            t += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator FadeInAndOut(Text text, Color targetColor, float waitTime = 1.0f)
    {
        float speed = 1.0f;
        Color startColor = text.color;
        float t = 0.0f;
        while (t < 1.0f)
        {
            text.color = Color.Lerp(startColor, targetColor, t);
            t += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
        
        yield return new WaitForSeconds(waitTime);

        t = 0.0f;
        while (t < 1.0f)
        {
            text.color = Color.Lerp(targetColor, Color.clear, t);
            t += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
    }

    public void ShowJourneyText(string text, Color targetColor)
    {
        StartCoroutine(FadeText(journeyText, Color.clear, 2.0f));
        journeyText.text = text;
        StartCoroutine(FadeInAndOut(journeyText, targetColor, 2.0f));
    }
}
