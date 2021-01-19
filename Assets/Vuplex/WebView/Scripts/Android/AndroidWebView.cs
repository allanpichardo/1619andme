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
#pragma warning disable CS0108
#pragma warning disable CS0067
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {

    /// <summary>
    /// The IWebView implementation used by 3D WebView for Android.
    /// This class also includes extra methods for Android-specific functionality.
    /// </summary>
    public class AndroidWebView : BaseWebView,
                                  IWebView,
                                  IWithMovablePointer,
                                  IWithPointerDownAndUp {

        public WebPluginType PluginType {
            get {
                return WebPluginType.Android;
            }
        }

        /// <summary>
        /// Indicates that a message was logged to the JavaScript console.
        /// </summary>
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessageLogged {
            add {
                _consoleMessageLogged += value;
                if (_consoleMessageLogged.GetInvocationList().Length == 1) {
                    _setConsoleMessageEventsEnabled(true);
                }
            }
            remove {
                _consoleMessageLogged -= value;
                if (_consoleMessageLogged.GetInvocationList().Length == 0) {
                    _setConsoleMessageEventsEnabled(false);
                }
            }
        }

        /// <summary>
        /// Indicates that the browser's render process terminated, either because it
        /// crashed or because the operating system killed it.
        /// </summary>
        /// <remarks>
        /// 3D WebView for Android internally uses the `android.webkit.WebView` system
        /// package as its browser engine. Android's documentation indicates that
        /// the browser's render process can terminate in some rare circumstances.
        /// This RenderProcessGone event indicates when that occurs so that the application
        /// can recover be destroying the existing webviews and creating new webviews.
        ///
        /// Sources:
        /// - [`android.webkit.WebViewClient.onRenderProcessGone()`](https://developer.android.com/reference/android/webkit/WebViewClient#onRenderProcessGone(android.webkit.WebView,%20android.webkit.RenderProcessGoneDetail))
        /// - [Termination Handling API (Android docs)](https://developer.android.com/guide/webapps/managing-webview#termination-handle)
        /// </remarks>
        public event EventHandler RenderProcessGone;

        [Obsolete("The ScriptAlert event has been renamed to ScriptAlerted. Please use ScriptAlerted instead.", true)]
        public event EventHandler<ScriptDialogEventArgs> ScriptAlert;

        /// <summary>
        /// Event raised when a script in the page calls `window.alert()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.alert()` will return
        /// immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until `ScriptDialogEventArgs.Continue()`
        /// is called.
        /// </remarks>
        public event EventHandler<ScriptDialogEventArgs> ScriptAlerted {
            add {
                if (_scriptAlertHandler != null) {
                    throw new InvalidOperationException("ScriptAlerted supports only one event handler. Please remove the existing handler before adding a new one.");
                }
                _scriptAlertHandler = value;
                _webView.Call("setScriptAlertHandler", new AndroidStringAndBoolDelegateCallback(_handleScriptAlert));
            }
            remove {
                if (_scriptAlertHandler == value) {
                    _scriptAlertHandler = null;
                    _webView.Call("setScriptAlertHandler", null);
                }
            }
        }

        /// <summary>
        /// Event raised when a script in the page calls `window.confirm()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.confirm()` will return
        /// `false` immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until `ScriptDialogEventArgs<bool>.Continue()`
        /// is called, and `window.confirm()` will return the value passed to `Continue()`.
        /// </remarks>
        public event EventHandler<ScriptDialogEventArgs<bool>> ScriptConfirmRequested {
            add {
                if (_scriptConfirmHandler != null) {
                    throw new InvalidOperationException("ScriptConfirmRequested supports only one event handler. Please remove the existing handler before adding a new one.");
                }
                _scriptConfirmHandler = value;
                _webView.Call("setScriptConfirmHandler", new AndroidStringAndBoolDelegateCallback(_handleScriptConfirm));
            }
            remove {
                if (_scriptConfirmHandler == value) {
                    _scriptConfirmHandler = null;
                    _webView.Call("setScriptConfirmHandler", null);
                }
            }
        }

        public static AndroidWebView Instantiate() {

            return (AndroidWebView) new GameObject().AddComponent<AndroidWebView>();
        }

        public override void Init(Texture2D viewportTexture, float width, float height, Texture2D videoTexture) {

            AssertWebViewIsAvailable();
            base.Init(viewportTexture, width, height, videoTexture);
            _webView = new AndroidJavaObject(
                FULL_CLASS_NAME,
                gameObject.name,
                viewportTexture.GetNativeTexturePtr().ToInt32(),
                _nativeWidth,
                _nativeHeight,
                SystemInfo.graphicsMultiThreaded,
                videoTexture != null
            );
        }

        internal static void AssertWebViewIsAvailable() {

            if (!IsWebViewAvailable()) {
                throw new WebViewUnavailableException("The Android WebView package is currently unavailable. This is rare but can occur if it's not installed on the system or is currently being updated.");
            }
        }

        public override void Blur() {

            _assertValidState();
            _webView.Call("blur");
        }

        public override void CanGoBack(Action<bool> callback) {

            _assertValidState();
            _webView.Call("canGoBack", new AndroidBoolCallback(callback));
        }

        public override void CanGoForward(Action<bool> callback) {

            _assertValidState();
            _webView.Call("canGoForward", new AndroidBoolCallback(callback));
        }

        /// <summary>
        /// Overrides `BaseWebView.CaptureScreenshot()` because it doesn't work
        /// with Android OES textures.
        /// </summary>
        public override void CaptureScreenshot(Action<byte[]> callback) {

            _assertValidState();
            _webView.Call("captureScreenshot", new AndroidByteArrayCallback(callback));
        }

        public static void ClearAllData() {

            _class.CallStatic("clearAllData");
        }

        /// <summary>
        /// Clears the webview's back / forward navigation history.
        /// </summary>
        public void ClearHistory() {

            _assertValidState();
            _webView.Call("clearHistory");
        }

        public override void Click(Vector2 point) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("click", nativeX, nativeY);
        }

        public override void DisableViewUpdates() {

            _assertValidState();
            _webView.Call("disableViewUpdates");
            _viewUpdatesAreEnabled = false;
        }

        public override void Dispose() {

            _assertValidState();
            // Cancel the render if it has been scheduled via GL.IssuePluginEvent().
            WebView_removePointer(_webView.GetRawObject());
            IsDisposed = true;
            _webView.Call("destroy");
            _webView.Dispose();
            Destroy(gameObject);
        }

        public override void EnableViewUpdates() {

            _assertValidState();
            _webView.Call("enableViewUpdates");
            _viewUpdatesAreEnabled = true;
        }

        public override void ExecuteJavaScript(string javaScript, Action<string> callback) {

            _assertValidState();
            var nativeCallback = callback == null ? null : new AndroidStringCallback(callback);
            _webView.Call("executeJavaScript", javaScript, nativeCallback);
        }

        public override void Focus() {

            _assertValidState();
            _webView.Call("focus");
        }

        public static string GetGraphicsApiErrorMessage(GraphicsDeviceType graphicsDeviceType) {

            var isValid = graphicsDeviceType == GraphicsDeviceType.OpenGLES3 || graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
            if (isValid) {
                return null;
            }
            return String.Format("Unsupported graphics API: 3D WebView for Android requires OpenGLES3 or OpenGLES2, but the graphics API in use is {0}. Please go to Player Settings and set \"Graphics APIs\" to OpenGLES3 or OpenGLES2.", graphicsDeviceType);
        }

        /// <summary>
        /// Overrides `BaseWebView.GetRawTextureData()` because it's slow on Android.
        /// </summary>
        public override void GetRawTextureData(Action<byte[]> callback) {

            _assertValidState();
            _webView.Call("getRawTextureData", new AndroidByteArrayCallback(callback));
        }

        public static void GloballySetUserAgent(bool mobile) {

            _class.CallStatic("globallySetUserAgent", mobile);
        }

        public static void GloballySetUserAgent(string userAgent) {

            _class.CallStatic("globallySetUserAgent", userAgent);
        }

        [Obsolete("AndroidWebView.GloballyUseAlternativeInputEventSystem() has been removed. Please use AndroidWebView.SetAlternativePointerInputSystemEnabled() and/or SetAlternativeKeyboardInputSystemEnabled() instead.", true)]
        public static void GloballyUseAlternativeInputEventSystem(bool useAlternativeInputEventSystem) {}

        public override void GoBack() {

            _assertValidState();
            _webView.Call("goBack");
        }

        public override void GoForward() {

            _assertValidState();
            _webView.Call("goForward");
        }

        public override void HandleKeyboardInput(string input) {

            _assertValidState();
            _webView.Call("handleKeyboardInput", input);
        }

        /// <summary>
        /// Indicates whether native video rendering is available for the current
        /// version of Android and is enabled.
        /// </summary>
        /// <remarks>
        /// Native video rendering is available in Android API level 23 and above.
        /// If native video rendering isn't supported (i.e. the Android version is
        /// lower than 23), then the AndroidWebView plugin will use a fallback video
        /// implementation to support basic video playback.
        /// </remarks>
        public static bool IsUsingNativeVideoRendering() {

            return _class.CallStatic<bool>("isUsingNativeVideoRendering");
        }

        /// <summary>
        /// Indicates whether the Android WebView package is installed on the system and available.
        /// </summary>
        /// <remarks>
        /// 3D WebView internally depends on Android's WebView package, which is normally installed
        /// as part of the operating system. In rare circumstances, the Android WebView package may be unavailable.
        /// For example, this can happen if the user used developer tools to delete the WebView package
        /// or if [updates to the WebView package are currently being installed](https://bugs.chromium.org/p/chromium/issues/detail?id=506369) .
        /// </remarks>
        public static bool IsWebViewAvailable() {

            if (_webViewPackageIsAvailable == null) {
                _webViewPackageIsAvailable = _class.CallStatic<bool>("isWebViewAvailable");
            }
            return (bool)_webViewPackageIsAvailable;
        }

        public override void LoadHtml(string html) {

            _assertValidState();
            _webView.Call("loadHtml", html);
        }

        /// <summary>
        /// Like `LoadHtml(string html)`, but also allows a virtual base URL
        /// to be specified.
        /// </summary>
        public void LoadHtml(string html, string baseUrl) {

            _assertValidState();
            _webView.Call("loadHtml", html, baseUrl);
        }

        public override void LoadUrl(string url) {

            _assertValidState();
            _webView.Call("loadUrl", _transformStreamingAssetsUrlIfNeeded(url));
        }

        public override void LoadUrl(string url, Dictionary<string, string> additionalHttpHeaders) {

            _assertValidState();
            if (additionalHttpHeaders == null) {
                LoadUrl(url);
            } else {
                var map = _convertDictionaryToJavaMap(additionalHttpHeaders);
                _webView.Call("loadUrl", url, map);
            }
        }

        /// <see cref="IWithMovablePointer"/>
        public void MovePointer(Vector2 point) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("movePointer", nativeX, nativeY);
        }

        /// <summary>
        /// Pauses processing, media, and rendering for this webview instance
        /// until `Resume()` is called.
        /// </summary>
        public void Pause() {

            _assertValidState();
            _webView.Call("pause");
        }

        /// <summary>
        /// Pauses processing, media, and rendering for all webview instances.
        /// This method is automatically called by the plugin when the application
        /// is paused.
        /// </summary>
        public static void PauseAll() {

            _class.CallStatic("pauseAll");
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point) {

            _pointerDown(point, MouseButton.Left, 1);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerDown(point, options.Button, options.ClickCount);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point) {

            _pointerUp(point, MouseButton.Left, 1);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerUp(point, options.Button, options.ClickCount);
        }

        /// <summary>
        /// Loads the given URL using an HTTP POST request and the given
        /// application/x-www-form-urlencoded data.
        /// </summary>
        /// <example>
        /// webView.PostUrl("https://postman-echo.com/post", Encoding.Unicode.GetBytes("foo=bar"));
        /// </example>
        public void PostUrl(string url, byte[] data) {

            _assertValidState();
            _webView.Call("postUrl", url, data);
        }

        public override void Reload() {

            _assertValidState();
            _webView.Call("reload");
        }

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `Pause().`
        /// </summary>
        public void Resume() {

            _assertValidState();
            _webView.Call("resume");
        }

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `PauseAll().` This method
        /// is automatically called by the plugin when the application resumes after
        /// being paused.
        /// </summary>
        public static void ResumeAll() {

            _class.CallStatic("resumeAll");
        }

        public override void Scroll(Vector2 scrollDelta) {

            _assertValidState();
            var deltaX = (int)(scrollDelta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(scrollDelta.y * _numberOfPixelsPerUnityUnit);
            _webView.Call("scroll", deltaX, deltaY);
        }

        public override void Scroll(Vector2 scrollDelta, Vector2 point) {

            _assertValidState();
            var deltaX = (int)(scrollDelta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(scrollDelta.y * _numberOfPixelsPerUnityUnit);
            var pointerX = (int)(point.x * _nativeWidth);
            var pointerY = (int)(point.y * _nativeHeight);
            _webView.Call("scroll", deltaX, deltaY, pointerX, pointerY);
        }

        public static void SetAlternativeKeyboardInputSystemEnabled(bool enabled) {

            _class.CallStatic("setAlternativeKeyboardInputSystemEnabled", enabled);
        }

        /// <summary>
        /// By default, 3D WebView dispatches pointer (a.k.a mouse) events to the
        /// browser engine in a way that accurately mimics the functionality of
        /// a desktop browser. This works great in most cases, but on some systems
        /// (i.e. Oculus Quest 2), the system version of Chromium is buggy and out-of-date,
        /// which can lead to issues where pointer events aren't dispatched accurately.
        /// In those cases, this method can be used to enable an alternative pointer
        /// input system that is less flexible but doesn't suffer from the Chromium
        /// bugs. This method is called automatically by AndroidWebPlugin.cs when
        /// running on Oculus Quest 2. Note that calling this method effectively disables
        /// the ability to trigger hover or drag events with `MovePointer()`.
        /// </summary>
        public static void SetAlternativePointerInputSystemEnabled(bool enabled) {

            _class.CallStatic("setAlternativePointerInputSystemEnabled", enabled);
        }

        /// <summary>
        /// By default, web pages cannot access the device's
        /// camera or microphone via JavaScript, even if the user has granted
        /// the app permission to use them. Invoking `SetAudioAndVideoCaptureEnabled(true)` allows
        /// **all web pages** to access the camera and microphone if the user has
        /// granted the app permission to use them via the standard Android permission dialogs.
        /// </summary>
        /// <remarks>
        /// This is useful, for example, to enable WebRTC support.
        /// In addition to calling this method, the application must include the following Android
        /// permissions in its AndroidManifest.xml and also request the permissions at runtime.
        /// - android.permission.RECORD_AUDIO
        /// - android.permission.MODIFY_AUDIO_SETTINGS
        /// - android.permission.CAMERA
        /// </remarks>
        public static void SetAudioAndVideoCaptureEnabled(bool enabled) {

            _class.CallStatic("setAudioAndVideoCaptureEnabled", enabled);
        }

        public static void SetClickCorrectionEnabled(bool enabled) {

            _class.CallStatic("setClickCorrectionEnabled", enabled);
        }

        /// <summary>
        /// By default, `AndroidWebView` allows requests for custom schemes (ex: myapp://myaction?data=foo).
        /// However, if you want to override this behavior, you can disable
        /// custom URI schemes with this method.
        /// </summary>
        public static void SetCustomUriSchemesEnabled(bool enabled) {

            _class.CallStatic("setCustomUriSchemesEnabled", enabled);
        }

        /// <summary>
        /// Normally, the native `android.webkit.WebView` instance redraws itself whenever
        /// the web content has changed. However on some systems (like the Oculus Quest 2),
        /// the operating system has a bug where this drawing does not occur
        /// automatically. In those cases, this  method must be called to make it so
        /// the webview is forced to redraw itself every frame. This method is automatically called
        /// by AndroidWebPlugin.cs for Oculus Quest 2.
        /// </summary>
        public static void SetForceDrawEnabled(bool enabled) {

            _class.CallStatic("setForceDrawEnabled", enabled);
        }

        /// <summary>
        /// By default, web pages cannot access the device's
        /// geolocation via JavaScript, even if the user has granted
        /// the app permission to access location. Invoking `SetGeolocationPermissionEnabled(true)` allows
        /// **all web pages** to access the geolocation if the user has
        /// granted the app location permissions via the standard Android permission dialogs.
        /// </summary>
        /// <remarks>
        /// The following Android permissions must be included in the app's AndroidManifest.xml
        /// and also requested by the application at runtime:
        /// - android.permission.ACCESS_COARSE_LOCATION
        /// - android.permission.ACCESS_FINE_LOCATION
        /// </remarks>
        public static void SetGeolocationPermissionEnabled(bool enabled) {

            _class.CallStatic("setGeolocationPermissionEnabled", enabled);
        }

        public static void SetIgnoreCertificateErrors(bool ignore) {

            _class.CallStatic("setIgnoreCertificateErrors", ignore);
        }

        [Obsolete("AndroidWebView.SetIgnoreSslErrors() is now deprecated. Please use Web.SetIgnoreCertificateErrors() instead.")]
        public static void SetIgnoreSslErrors(bool ignore) {

            SetIgnoreCertificateErrors(ignore);
        }

        /// <summary>
        /// Sets the initial scale for web content, where 1.0 is the default scale.
        /// </summary>
        public void SetInitialScale(float scale) {

            _assertValidState();
            _webView.Call("setInitialScale", scale);
        }

        /// <summary>
        /// By default, AndroidWebView prevents JavaScript from auto-playing sound
        /// from most sites unless the user has first interacted with the page.
        /// You can call this method to disable or re-enable enforcement of this auto-play policy.
        /// </summary>
        public void SetMediaPlaybackRequiresUserGesture(bool mediaPlaybackRequiresUserGesture) {

            _assertValidState();
            _webView.Call("setMediaPlaybackRequiresUserGesture", mediaPlaybackRequiresUserGesture);
        }

        [Obsolete("AndroidWebView.SetNativeKeyboardEnabled() is now deprecated. Please use Web.SetTouchScreenKeyboardEnabled() instead.")]
        public static void SetNativeKeyboardEnabled(bool enabled) {

            SetTouchScreenKeyboardEnabled(enabled);
        }

        /// <summary>
        /// Enables or disables native video rendering on versions of Android
        /// that support native video rendering.
        /// </summary>
        /// <remarks>
        /// The default is enabled. If disabled, then the `AndroidWebView`
        /// plugin will use a fallback video implementation to support basic
        /// video playback. This method is automatically called when the
        /// Oculus VR SDK is enabled, because the Oculus Go and Quest
        /// headsets don't support native video rendering.
        /// </remarks>
        public static void SetNativeVideoRenderingEnabled(bool enabled) {

            _class.CallStatic("setNativeVideoRenderingEnabled", enabled);
        }

        public static void SetStorageEnabled(bool enabled) {

            _class.CallStatic("setStorageEnabled", enabled);
        }

        /// <summary>
        /// Sets the `android.view.Surface` to which the webview renders.
        /// This can be used, for example, to render to an Oculus
        /// [OVROverlay](https://developer.oculus.com/reference/unity/1.30/class_o_v_r_overlay).
        /// After this method is called, the webview no longer renders
        /// to its original texture and instead renders to the given surface.
        /// </summary>
        /// <example>
        /// var surface = ovrOverlay.externalSurfaceObject();
        /// // Set the resolution to 1 px / Unity unit
        /// // to make it easy to specify the size in pixels.
        /// webView.SetResolution(1);
        /// // Or if the webview is attached to a prefab, call WebViewPrefab.Resize()
        /// webView.WebView.Resize(surface.externalSurfaceWidth(), surface.externalSurfaceHeight());
        /// #if UNITY_ANDROID && !UNITY_EDITOR
        ///     (webView as AndroidWebView).SetSurface(surface);
        /// #endif
        /// </example>
        public void SetSurface(IntPtr surface) {

            _assertValidState();
            var surfaceObject = _convertIntPtrToAndroidJavaObject(surface);
            _webView.Call("setSurface", surfaceObject);
        }

        public static void SetTouchScreenKeyboardEnabled(bool enabled) {

            _class.CallStatic("setTouchScreenKeyboardEnabled", enabled);
        }

        /// <summary>
        /// Like `Web.SetUserAgent(bool mobile)`, except it sets the user-agent
        /// for a single webview instance instead of setting it globally.
        /// </summary>
        /// <remarks>
        /// If you globally set a default user-agent using `Web.SetUserAgent()`,
        /// you can still use this method to override the user-agent for a
        /// single webview instance.
        /// </remarks>
        public void SetUserAgent(bool mobile) {

            _assertValidState();
            _webView.Call("setUserAgent", mobile);
        }

        /// <summary>
        /// Like `Web.SetUserAgent(string userAgent)`, except it sets the user-agent
        /// for a single webview instance instead of setting it globally.
        /// </summary>
        /// <remarks>
        /// If you globally set a default user-agent using `Web.SetUserAgent()`,
        /// you can still use this method to override the user-agent for a
        /// single webview instance.
        /// </remarks>
        public void SetUserAgent(string userAgent) {

            _assertValidState();
            _webView.Call("setUserAgent", userAgent);
        }

        [Obsolete("AndroidWebView.UseAlternativeInputEventSystem() has been removed. Please use AndroidWebView.SetAlternativePointerInputSystemEnabled() and/or SetAlternativeKeyboardInputSystemEnabled() instead.", true)]
        public void UseAlternativeInputEventSystem(bool useAlternativeInputEventSystem) {}

        /// <summary>
        /// Zooms in or out by the given factor, which is multiplied by the current zoom level
        /// to reach the new zoom level.
        /// </summary>
        /// <remarks>
        /// Note that the zoom level gets reset when a new page is loaded.
        /// </remarks>
        /// <param name="zoomFactor">
        /// The zoom factor to apply in the range from 0.01 to 100.0.
        /// </param>
        public void ZoomBy(float zoomFactor) {

            _assertValidState();
            _webView.Call("zoomBy", zoomFactor);
        }

        public override void ZoomIn() {

            _assertValidState();
            _webView.Call("zoomIn");
        }

        public override void ZoomOut() {

            _assertValidState();
            _webView.Call("zoomOut");
        }

        // Get a reference to AndroidJavaObject's hidden constructor that takes
        // the IntPtr for a jobject as a parameter.
        readonly static ConstructorInfo _androidJavaObjectIntPtrConstructor = typeof(AndroidJavaObject).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new []{ typeof(IntPtr) },
            null
        );
        internal static AndroidJavaClass _class = new AndroidJavaClass(FULL_CLASS_NAME);
        event EventHandler<ConsoleMessageEventArgs> _consoleMessageLogged;
        const string FULL_CLASS_NAME = "com.vuplex.webview.WebView";
        EventHandler<ScriptDialogEventArgs> _scriptAlertHandler;
        EventHandler<ScriptDialogEventArgs<bool>> _scriptConfirmHandler;
        internal AndroidJavaObject _webView;
        static bool? _webViewPackageIsAvailable = null;

        AndroidJavaObject _convertDictionaryToJavaMap(Dictionary<string, string> dictionary) {

            AndroidJavaObject map = new AndroidJavaObject("java.util.HashMap");
            IntPtr putMethod = AndroidJNIHelper.GetMethodID(map.GetRawClass(), "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
            foreach (var entry in dictionary) {
                AndroidJNI.CallObjectMethod(
                    map.GetRawObject(),
                    putMethod,
                    AndroidJNIHelper.CreateJNIArgArray(new object[] { entry.Key, entry.Value })
                );
            }
            return map;
        }

        static AndroidJavaObject _convertIntPtrToAndroidJavaObject(IntPtr jobject) {

            if (jobject == IntPtr.Zero) {
                return null;
            }
            return (AndroidJavaObject) _androidJavaObjectIntPtrConstructor.Invoke(new object[] { jobject });
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleConsoleMessageLogged(string levelAndMessage) {

            var handler = _consoleMessageLogged;
            if (handler == null) {
                return;
            }
            var separatedLevelAndMessage = levelAndMessage.Split(new char[] { ',' }, 2);
            var level = _parseConsoleMessageLevel(separatedLevelAndMessage[0]);
            var message = separatedLevelAndMessage[1];
            handler(this, new ConsoleMessageEventArgs(level, message));
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        protected virtual void HandleInitialVideoPlayRequest(string serializedVideo) {

            _assertValidState();
            var video = JsonUtility.FromJson<Video>(serializedVideo);
            var nativeVideoPlayer = _webView.Call<AndroidJavaObject>("getOrCreateVideoPlayer", serializedVideo, _videoTexture.GetNativeTexturePtr().ToInt32());
            nativeVideoPlayer.Call("play", video.videoUrl);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleRenderProcessGone() {

            var handler = RenderProcessGone;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        void _handleScriptAlert(string message, Action<bool> continueCallback) {

            _scriptAlertHandler(this, new ScriptDialogEventArgs(message, () => continueCallback(true)));
        }

        void _handleScriptConfirm(string message, Action<bool> continueCallback) {

            _scriptConfirmHandler(this, new ScriptDialogEventArgs<bool>(message, continueCallback));
        }

        void OnEnable() {

            // Start the coroutine from OnEnable so that the coroutine
            // is restarted if the object is deactivated and then reactivated.
            StartCoroutine(_renderPluginOncePerFrame());
        }

        ConsoleMessageLevel _parseConsoleMessageLevel(string levelString) {

            switch (levelString) {
                case "DEBUG":
                    return ConsoleMessageLevel.Debug;
                case "ERROR":
                    return ConsoleMessageLevel.Error;
                case "LOG":
                    return ConsoleMessageLevel.Log;
                case "WARNING":
                    return ConsoleMessageLevel.Warning;
                default:
                    Debug.LogWarning("Unrecognized console message level: " + levelString);
                    return ConsoleMessageLevel.Log;
            }
        }

        void _pointerDown(Vector2 point, MouseButton mouseButton, int clickCount) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("pointerDown", nativeX, nativeY, (int)mouseButton, clickCount);
        }

        void _pointerUp(Vector2 point, MouseButton mouseButton, int clickCount) {

            _assertValidState();
            var nativeX = (int)(point.x * _nativeWidth);
            var nativeY = (int)(point.y * _nativeHeight);
            _webView.Call("pointerUp", nativeX, nativeY, (int)mouseButton, clickCount);
        }

        IEnumerator _renderPluginOncePerFrame() {
            while (true) {
                // Wait until all frame rendering is done
                yield return new WaitForEndOfFrame();

                if (!_viewUpdatesAreEnabled || IsDisposed || _webView == null) {
                    continue;
                }
                var nativeWebViewPtr = _webView.GetRawObject();
                if (nativeWebViewPtr != IntPtr.Zero) {
                    int pointerId = WebView_depositPointer(nativeWebViewPtr);
                    GL.IssuePluginEvent(WebView_getRenderFunction(), pointerId);
                }
            }
        }

        protected override void _resize() {

            // Only trigger a resize if the webview has been initialized
            if (_viewportTexture) {
                _assertValidState();
                Utils.ThrowExceptionIfAbnormallyLarge(_nativeWidth, _nativeHeight);
                _webView.Call("resize", _nativeWidth, _nativeHeight);
            }
        }

        private void _setConsoleMessageEventsEnabled(bool enabled) {

            _assertValidState();
            _webView.Call("setConsoleMessageEventsEnabled", enabled);
        }

        [DllImport(_dllName)]
        static extern IntPtr WebView_getRenderFunction();

        [DllImport(_dllName)]
        static extern int WebView_depositPointer(IntPtr pointer);

        [DllImport(_dllName)]
        static extern void WebView_removePointer(IntPtr pointer);
    }
}
#endif
