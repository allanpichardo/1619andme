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
#if UNITY_ANDROID && !UNITY_EDITOR
#pragma warning disable CS0618
using System;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
    using UnityEngine.XR;
#else
    using XRSettings = UnityEngine.VR.VRSettings;
#endif

namespace Vuplex.WebView {

    class AndroidWebPlugin : MonoBehaviour,
                             IWebPlugin,
                             IPluginWithTouchScreenKeyboard {

        public static AndroidWebPlugin Instance {
            get {
                if (_instance == null) {
                    _instance = (AndroidWebPlugin) new GameObject("AndroidWebPlugin").AddComponent<AndroidWebPlugin>();
                    DontDestroyOnLoad(_instance.gameObject);
                    // Native video rendering does not work on standalone VR headsets like
                    // Oculus Go, Oculus Quest, or HTC Vive Focus,
                    // so disable it in order to use fallback video rendering.
                    var isStandaloneVrHeadset = XRSettings.enabled &&
                                                (XRSettings.loadedDeviceName == "Oculus" ||
                                                XRSettings.loadedDeviceName == "MockHMD"); // HTC Vive Focus
                    if (isStandaloneVrHeadset) {
                        Debug.LogWarning("3D WebView for Android doesn't support native video and WebGL on standalone VR headsets, so a fallback video implementation will be used instead. For standalone VR headsets, it is recommended to instead use 3D WebView for Android with Gecko Engine: https://developer.vuplex.com/webview/android-comparison");
                        AndroidWebView.SetNativeVideoRenderingEnabled(false);
                    }

                    #if UNITY_2017_2_OR_NEWER
                        if (SystemInfo.deviceName == "Oculus Quest 2") {
                            // The Quest 2's operating system has a bug where the webview doesn't
                            // draw itself automatically when it updates, so we must force drawing.
                            AndroidWebView.SetForceDrawEnabled(true);
                            // The Quest 2's version of Chromium also has a bug where it often dispatches
                            // pointer events at the wrong coordinates when using the default pointer
                            // input system, so the alternative pointer input system must be used instead.
                            AndroidWebView.SetAlternativePointerInputSystemEnabled(true);
                        }
                    #endif
                }
                return _instance;
            }
        }

        public void ClearAllData() {

            AndroidWebView.ClearAllData();
        }

        public void CreateTexture(float width, float height, Action<Texture2D> callback) {

            AndroidTextureCreator.Instance.CreateTexture(width, height, callback);
        }

        public void CreateMaterial(Action<Material> callback) {

            CreateTexture(1, 1, texture => {
                var materialName = "AndroidViewportMaterial";
                #if UNITY_2017_2_OR_NEWER
                    var singlePassStereoRenderingIsEnabled = XRSettings.enabled && XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes;
                    if (singlePassStereoRenderingIsEnabled) {
                        materialName = "AndroidSinglePassViewportMaterial";
                    }
                #endif
                // Construct a new material, because Resources.Load<T>() returns a singleton.
                var material = new Material(Resources.Load<Material>(materialName));
                material.mainTexture = texture;
                callback(material);
            });
        }

        public void CreateVideoMaterial(Action<Material> callback) {

            if (AndroidWebView.IsUsingNativeVideoRendering()) {
                // Video is rendered natively onto the web texture, so a separate video
                // texture isn't required.
                callback(null);
                return;
            }

            // Since native video rendering isn't supported in this Android version, fallback
            // to rendering video onto a separate texture.
            CreateTexture(1, 1, texture => {
                var materialName = "AndroidVideoMaterial";
                #if UNITY_2017_2_OR_NEWER
                    var singlePassStereoRenderingIsEnabled = XRSettings.enabled && XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes;
                    if (singlePassStereoRenderingIsEnabled) {
                        materialName = "AndroidSinglePassVideoMaterial";
                    }
                #endif
                var material = new Material(Resources.Load<Material>(materialName));
                material.mainTexture = texture;
                callback(material);
            });
        }

        public virtual IWebView CreateWebView() {

            return AndroidWebView.Instantiate();
        }

        public void SetIgnoreCertificateErrors(bool ignore) {

            AndroidWebView.SetIgnoreCertificateErrors(ignore);
        }

        /// <see cref="IPluginWithTouchScreenKeyboard"/>
        public void SetTouchScreenKeyboardEnabled(bool enabled) {

            AndroidWebView.SetTouchScreenKeyboardEnabled(enabled);
        }

        public void SetStorageEnabled(bool enabled) {

            AndroidWebView.SetStorageEnabled(enabled);
        }

        public void SetUserAgent(bool mobile) {

            AndroidWebView.GloballySetUserAgent(mobile);
        }

        public void SetUserAgent(string userAgent) {

            AndroidWebView.GloballySetUserAgent(userAgent);
        }

        static AndroidWebPlugin _instance;

        /// <summary>
        /// Automatically pause web processing and media playback
        /// when the app is paused and resume it when the app is resumed.
        /// </summary>
        void OnApplicationPause(bool isPaused) {

            if (isPaused) {
                AndroidWebView.PauseAll();
            } else {
                AndroidWebView.ResumeAll();
            }
        }
    }
}
#endif
