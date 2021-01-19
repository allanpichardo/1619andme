/**
* Copyright (c) 2020 Vuplex Inc. All rights reserved.
*
* Licensed under the Vuplex Commercial Software Library License, you may
* not use this file except in compliance with the License. You may obtain
* a copy of the License at
*
*     https://vuplex.com/commercial-library-license
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using UnityEngine;

namespace Vuplex.WebView.Demos {

    /// <summary>
    /// Sets up the CanvasPopupDemo scene, which demonstrates how to use
    /// the IWithPopups interface with WebViewPrefab.
    /// </summary>
    class PopupDemo : MonoBehaviour {

        WebViewPrefab _focusedPrefab;
        HardwareKeyboardListener _hardwareKeyboardListener;

        void Start() {

            // Create a 0.6 x 0.3 webview for the main web content.
            var mainWebViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.3f);
            mainWebViewPrefab.transform.parent = transform;
            mainWebViewPrefab.transform.localPosition = new Vector3(0, 0, 0.4f);
            mainWebViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);

            _focusedPrefab = mainWebViewPrefab;
            mainWebViewPrefab.Initialized += (s, e) => {
                var webViewWithPopups = mainWebViewPrefab.WebView as IWithPopups;
                if (webViewWithPopups == null) {
                    mainWebViewPrefab.WebView.LoadHtml(NOT_SUPPORTED_HTML);
                    return;
                }

                Debug.Log("Loading Pinterest as an example because it uses popups for third party login. Click 'Login', then select Facebook or Google to open a popup for authentication.");
                mainWebViewPrefab.WebView.LoadUrl("https://pinterest.com");

                webViewWithPopups.SetPopupMode(PopupMode.LoadInNewWebView);
                webViewWithPopups.PopupRequested += (webView, eventArgs) => {
                    Debug.Log("Popup opened with URL: " + eventArgs.Url);
                    var popupPrefab = WebViewPrefab.Instantiate(eventArgs.WebView);
                    _focusedPrefab = popupPrefab;
                    popupPrefab.transform.parent = transform;
                    // Place the popup in front of the main webview.
                    popupPrefab.transform.localPosition = new Vector3(0, 0, 0.39f);
                    popupPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
                    popupPrefab.Initialized += (sender, initializedEventArgs) => {
                        popupPrefab.WebView.CloseRequested += (popupWebView, closeEventArgs) => {
                            Debug.Log("Closing the popup");
                            _focusedPrefab = mainWebViewPrefab;
                            popupPrefab.Destroy();
                        };
                    };
                };
            };

            _setUpKeyboards();
        }

        void _setUpKeyboards() {

            // Send keys from the hardware (USB or Bluetooth) keyboard to the webview.
            // Use separate `KeyDown()` and `KeyUp()` methods if the webview supports
            // it, otherwise just use `IWebView.HandleKeyboardInput()`.
            // https://developer.vuplex.com/webview/IWithKeyDownAndUp
            _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
            _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
                var webViewWithKeyDown = _focusedPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyDown == null) {
                    _focusedPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
                } else {
                    webViewWithKeyDown.KeyDown(eventArgs.Value, eventArgs.Modifiers);
                }
            };
            _hardwareKeyboardListener.KeyUpReceived += (sender, eventArgs) => {
                var webViewWithKeyUp = _focusedPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyUp != null) {
                    webViewWithKeyUp.KeyUp(eventArgs.Value, eventArgs.Modifiers);
                }
            };

            // Also add an on-screen keyboard under the main webview.
            var keyboard = Keyboard.Instantiate();
            keyboard.transform.parent = _focusedPrefab.transform;
            keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
            keyboard.transform.localEulerAngles = new Vector3(0, 0, 0);
            keyboard.InputReceived += (sender, eventArgs) => {
                _focusedPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
            };
        }

        const string NOT_SUPPORTED_HTML = @"
            <body>
                <style>
                    body {
                        font-family: sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        line-height: 1.25;
                    }
                    div {
                        max-width: 80%;
                    }
                    li {
                        margin: 10px 0;
                    }
                </style>
                <div>
                    <p>
                        Sorry, but this 3D WebView package doesn't support yet the <a href='https://developer.vuplex.com/webview/IWithPopups'>IWithPopups</a> interface. Current packages that support popups:
                    </p>
                    <ul>
                        <li>
                            <a href='https://developer.vuplex.com/webview/StandaloneWebView'>3D WebView for Windows and macOS</a>
                        </li>
                        <li>
                            <a href='https://developer.vuplex.com/webview/AndroidGeckoWebView'>3D WebView for Android with Gecko Engine</a>
                        </li>
                    </ul>
                </div>
            </body>
        ";
    }
}
