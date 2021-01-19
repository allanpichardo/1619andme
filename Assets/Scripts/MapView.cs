using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

public class MapView : MonoBehaviour
{
    
    public WebViewPrefab _webViewPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        if (_webViewPrefab)
        {
            _webViewPrefab.Initialized += (sender, e) => {
                Debug.Log("Web View: Initialized()");
                _webViewPrefab.WebView.LoadUrl("https://ftdg.allanpichardo.com");
            };
        }
    }

    private void Update()
    {
        if (_webViewPrefab)
        {
            Debug.Log($"Web View: isInitialized = {_webViewPrefab.WebView.IsInitialized}");
        }
    }
}
