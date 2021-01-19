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
    /// Sets up the SimpleWebViewDemo scene, which demonstrates the following:
    /// - displaying a `WebViewPrefab` created via the editor in world space
    /// - creating and hooking up an on-screen keyboard
    /// </summary>
    /// <remarks>
    /// This scene includes Unity's standalone input module, so
    /// you can click and scroll the webview using your touchscreen
    /// or mouse.
    ///
    /// You can also move the camera by holding down the control key on your
    /// keyboard and moving your mouse. When running on a device
    /// with a gyroscope, the gyroscope controls the camera rotation instead.
    ///
    /// `WebViewPrefab` handles standard Unity input events, so it works with
    /// a variety of third party input modules that extend Unity's `BaseInputModule`,
    /// like the input modules from the Google VR and Oculus VR SDKs.
    ///
    /// Here are some other examples that show how to use 3D WebView with popular SDKs:
    /// • Oculus (Quest, Go, Rift, and Gear VR): https://github.com/vuplex/oculus-webview-example
    /// • Hololens: https://github.com/vuplex/hololens-webview-example
    /// • AR Foundation : https://github.com/vuplex/ar-foundation-webview-example
    /// • Google VR (Cardboard and Daydream): https://github.com/vuplex/google-vr-webview-example
    /// </remarks>
    class SimpleWebViewDemo : MonoBehaviour {

        HardwareKeyboardListener _hardwareKeyboardListener;
        WebViewPrefab _webViewPrefab;

        void Start() {

            // The WebViewPrefab's `InitialUrl` property is set via the editor, so it
            // will automatically initialize itself with that URL.
            _webViewPrefab = GameObject.Find("WebViewPrefab").GetComponent<WebViewPrefab>();
            _setUpKeyboards();
        }

        void _setUpKeyboards() {

            // Send keys from the hardware (USB or Bluetooth) keyboard to the webview.
            // Use separate `KeyDown()` and `KeyUp()` methods if the webview supports
            // it, otherwise just use `IWebView.HandleKeyboardInput()`.
            // https://developer.vuplex.com/webview/IWithKeyDownAndUp
            _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
            _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
                var webViewWithKeyDown = _webViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyDown == null) {
                    _webViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
                } else {
                    webViewWithKeyDown.KeyDown(eventArgs.Value, eventArgs.Modifiers);
                }
            };
            _hardwareKeyboardListener.KeyUpReceived += (sender, eventArgs) => {
                var webViewWithKeyUp = _webViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyUp != null) {
                    webViewWithKeyUp.KeyUp(eventArgs.Value, eventArgs.Modifiers);
                }
            };

            // Also add an on-screen keyboard under the main webview.
            var keyboard = Keyboard.Instantiate();
            keyboard.transform.parent = _webViewPrefab.transform;
            keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
            keyboard.transform.localEulerAngles = new Vector3(0, 0, 0);
            keyboard.InputReceived += (sender, eventArgs) => {
                _webViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
            };
        }
    }
}
