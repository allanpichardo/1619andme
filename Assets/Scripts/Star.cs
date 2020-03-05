using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour
{
    private AudioPoint _audioPoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetAudioPoint(AudioPoint audioPoint)
    {
        this._audioPoint = audioPoint;
        Color color = ConvertAndroidColor( audioPoint.region.GetHashCode());
        this.GetComponent<Renderer>().material.SetColor("Color_45EDB685", color);
    }

    public AudioPoint GetAudioPoint()
    {
        return this._audioPoint;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public static Color ConvertAndroidColor(int aCol)
    {
        Color c = new Color();
        c.b = aCol & 255;
        c.g = (aCol >> 8) & 255;
        c.r = (aCol >> 16) & 255;
        return c;
    }
    
    
}
