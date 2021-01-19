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
// Only define BaseWebView.cs on supported platforms to avoid IL2CPP linking
// errors on unsupported platforms.
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || (UNITY_IOS && !VUPLEX_OMIT_IOS) || UNITY_WSA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
#if NET_4_6 || NET_STANDARD_2_0
    using System.Threading.Tasks;
#endif

namespace Vuplex.WebView {
    /// <summary>
    /// The base `IWebView` implementation, which is extended for each platform.
    /// </summary>
    public abstract class BaseWebView : MonoBehaviour {

        public event EventHandler CloseRequested;

        public event EventHandler<ProgressChangedEventArgs> LoadProgressChanged;

        public event EventHandler<EventArgs<string>> MessageEmitted  {
            add { _messageHandler.MessageEmitted += value; }
            remove { _messageHandler.MessageEmitted -= value; }
        }

        public event EventHandler PageLoadFailed;

        public event EventHandler<EventArgs<string>> TitleChanged  {
            add { _messageHandler.TitleChanged += value; }
            remove { _messageHandler.TitleChanged -= value; }
        }

        public event EventHandler<UrlChangedEventArgs> UrlChanged;

        public event EventHandler<EventArgs<Rect>> VideoRectChanged  {
            add { _messageHandler.VideoRectChanged += value; }
            remove { _messageHandler.VideoRectChanged -= value; }
        }

        public bool IsDisposed { get; protected set; }

        public bool IsInitialized { get; private set; }

        public List<string> PageLoadScripts {
            get {
                return _pageLoadScripts;
            }
        }

        public float Resolution {
            get {
                return _numberOfPixelsPerUnityUnit;
            }
        }

        public Vector2 Size {
            get {
                return new Vector2(_width, _height);
            }
        }

        public Vector2 SizeInPixels {
            get {
                return new Vector2(_nativeWidth, _nativeHeight);
            }
        }

        public Texture2D Texture {
            get {
                return _viewportTexture;
            }
        }

        public string Url { get; private set; }

        public Texture2D VideoTexture {
            get {
                return _videoTexture;
            }
        }

        public void Init(Texture2D texture, float width, float height) {

            Init(texture, width, height, null);
        }

        public virtual void Init(Texture2D viewportTexture, float width, float height, Texture2D videoTexture) {

            if (IsInitialized) {
                throw new InvalidOperationException("Init() cannot be called on a webview that has already been initialized.");
            }
            _viewportTexture = viewportTexture;
            _videoTexture = videoTexture;
            // Assign the game object a unique name so that the native view can send it messages.
            gameObject.name = String.Format("WebView-{0}", Guid.NewGuid().ToString());
            _width = width;
            _height = height;
            Utils.ThrowExceptionIfAbnormallyLarge(_nativeWidth, _nativeHeight);
            _messageHandler.JavaScriptResultReceived += MessageHandler_JavaScriptResultReceived;
            _messageHandler.UrlChanged += MessageHandler_UrlChanged;
            IsInitialized = true;
            // Prevent the script from automatically being destroyed when a new scene is loaded.
            DontDestroyOnLoad(gameObject);
        }

        public virtual void Blur() {

            _assertValidState();
            WebView_blur(_nativeWebViewPtr);
        }

    #if NET_4_6 || NET_STANDARD_2_0
        public Task<bool> CanGoBack() {

            var task = new TaskCompletionSource<bool>();
            CanGoBack(task.SetResult);
            return task.Task;
        }

        public Task<bool> CanGoForward() {

            var task = new TaskCompletionSource<bool>();
            CanGoForward(task.SetResult);
            return task.Task;
        }
    #endif

        public virtual void CanGoBack(Action<bool> callback) {

            _assertValidState();
            _pendingCanGoBackCallbacks.Add(callback);
            WebView_canGoBack(_nativeWebViewPtr);
        }

        public virtual void CanGoForward(Action<bool> callback) {

            _assertValidState();
            _pendingCanGoForwardCallbacks.Add(callback);
            WebView_canGoForward(_nativeWebViewPtr);
        }

    #if NET_4_6 || NET_STANDARD_2_0
        public Task<byte[]> CaptureScreenshot() {

            var task = new TaskCompletionSource<byte[]>();
            CaptureScreenshot(task.SetResult);
            return task.Task;
        }
    #endif

        public virtual void CaptureScreenshot(Action<byte[]> callback) {

            var bytes = new byte[0];
            try {
                var texture = _getReadableTexture();
                bytes = ImageConversion.EncodeToPNG(texture);
                Destroy(texture);
            } catch (Exception e) {
                Debug.LogError("An exception occurred while capturing the screenshot: " + e);
            }
            callback(bytes);
        }

        public virtual void Click(Vector2 point) {

            _assertValidState();
            int nativeX = (int) (point.x * _nativeWidth);
            int nativeY = (int) (point.y * _nativeHeight);
            WebView_click(_nativeWebViewPtr, nativeX, nativeY);
        }

        public virtual void Click(Vector2 point, bool preventStealingFocus) {

            // On most platforms, the regular `Click()` method
            // doesn't steal focus.
            Click(point);
        }

        public static void ClearAllData() {

            WebView_clearAllData();
        }

        public static void CreateTexture(float width, float height, Action<Texture2D> callback) {

            int nativeWidth = (int)(width * Config.NumberOfPixelsPerUnityUnit);
            int nativeHeight = (int)(height * Config.NumberOfPixelsPerUnityUnit);
            Utils.ThrowExceptionIfAbnormallyLarge(nativeWidth, nativeHeight);
            var texture = new Texture2D(
                nativeWidth,
                nativeHeight,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Invoke the callback asynchronously in order to match the async
            // behavior that's required for Android.
            Dispatcher.RunOnMainThread(() => callback(texture));
        }

        public virtual void Copy() {

            _assertValidState();
            _getSelectedText(text => GUIUtility.systemCopyBuffer = text);
        }

        public virtual void Cut() {

            _assertValidState();
            _getSelectedText(text => {
                GUIUtility.systemCopyBuffer = text;
                HandleKeyboardInput("Backspace");
            });
        }

        public virtual void DisableViewUpdates() {

            _assertValidState();
            WebView_disableViewUpdates(_nativeWebViewPtr);
            _viewUpdatesAreEnabled = false;
        }

        public virtual void Dispose() {

            _assertValidState();
            IsDisposed = true;
            WebView_destroy(_nativeWebViewPtr);
            _nativeWebViewPtr = IntPtr.Zero;
            // To avoid a MissingReferenceException, verify that this script
            // hasn't already been destroyed prior to accessing gameObject.
            if (this != null) {
                Destroy(gameObject);
            }
        }

        public virtual void EnableViewUpdates() {

            _assertValidState();
            if (_currentViewportNativeTexture != IntPtr.Zero) {
                _viewportTexture.UpdateExternalTexture(_currentViewportNativeTexture);
            }
            WebView_enableViewUpdates(_nativeWebViewPtr);
            _viewUpdatesAreEnabled = true;
        }

    #if NET_4_6 || NET_STANDARD_2_0
        public Task<string> ExecuteJavaScript(string javaScript) {

            var task = new TaskCompletionSource<string>();
            ExecuteJavaScript(javaScript, task.SetResult);
            return task.Task;
        }
    #else
        public void ExecuteJavaScript(string javaScript) {

            ExecuteJavaScript(javaScript, null);
        }
    #endif

        public virtual void ExecuteJavaScript(string javaScript, Action<string> callback) {

            _assertValidState();
            string resultCallbackId = null;
            if (callback != null) {
                resultCallbackId = Guid.NewGuid().ToString();
                _pendingJavaScriptResultCallbacks[resultCallbackId] = callback;
            }
            WebView_executeJavaScript(_nativeWebViewPtr, javaScript, resultCallbackId);
        }

        public virtual void Focus() {

            _assertValidState();
            WebView_focus(_nativeWebViewPtr);
        }

    #if NET_4_6 || NET_STANDARD_2_0
        public Task<byte[]> GetRawTextureData() {

            var task = new TaskCompletionSource<byte[]>();
            GetRawTextureData(task.SetResult);
            return task.Task;
        }
    #endif


        public virtual void GetRawTextureData(Action<byte[]> callback) {

            var bytes = new byte[0];
            try {
                var texture = _getReadableTexture();
                bytes = texture.GetRawTextureData();
                Destroy(texture);
            } catch (Exception e) {
                Debug.LogError("An exception occurred while getting the raw texture data: " + e);
            }
            callback(bytes);
        }

        public static void GloballySetUserAgent(bool mobile) {

            var success = WebView_globallySetUserAgentToMobile(mobile);
            if (!success) {
                throw new InvalidOperationException(USER_AGENT_EXCEPTION_MESSAGE);
            }
        }

        public static void GloballySetUserAgent(string userAgent) {

            var success = WebView_globallySetUserAgent(userAgent);
            if (!success) {
                throw new InvalidOperationException(USER_AGENT_EXCEPTION_MESSAGE);
            }
        }

        public virtual void GoBack() {

            _assertValidState();
            WebView_goBack(_nativeWebViewPtr);
        }

        public virtual void GoForward() {

            _assertValidState();
            WebView_goForward(_nativeWebViewPtr);
        }

        public virtual void HandleKeyboardInput(string input) {

            _assertValidState();
            WebView_handleKeyboardInput(_nativeWebViewPtr, input);
        }

        public virtual void LoadHtml(string html) {

            _assertValidState();
            WebView_loadHtml(_nativeWebViewPtr, html);
        }

        public virtual void LoadUrl(string url) {

            _assertValidState();
            WebView_loadUrl(_nativeWebViewPtr, _transformStreamingAssetsUrlIfNeeded(url));
        }

        public virtual void LoadUrl(string url, Dictionary<string, string> additionalHttpHeaders) {

            _assertValidState();
            if (additionalHttpHeaders == null) {
                LoadUrl(url);
            } else {
                var headerStrings = additionalHttpHeaders.Keys.Select(key => String.Format("{0}: {1}", key, additionalHttpHeaders[key])).ToArray();
                var newlineDelimitedHttpHeaders = String.Join("\n", headerStrings);
                WebView_loadUrlWithHeaders(_nativeWebViewPtr, url, newlineDelimitedHttpHeaders);
            }
        }

        public virtual void Paste() {

            _assertValidState();
            var text = GUIUtility.systemCopyBuffer;
            foreach (var character in text) {
                HandleKeyboardInput(char.ToString(character));
            }
        }

        public void PostMessage(string data) {

            var escapedString = data.Replace("'", "\\'").Replace("\n", "\\n");
            ExecuteJavaScript(String.Format("vuplex._emit('message', {{ data: \'{0}\' }})", escapedString));
        }

        public virtual void Reload() {

            _assertValidState();
            WebView_reload(_nativeWebViewPtr);
        }

        public virtual void Resize(float width, float height) {

            if (IsDisposed || (width == _width && height == _height)) {
                return;
            }
            _width = width;
            _height = height;
            _resize();
        }

        public virtual void Scroll(Vector2 delta) {

            _assertValidState();
            var deltaX = (int)(delta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(delta.y * _numberOfPixelsPerUnityUnit);
            WebView_scroll(_nativeWebViewPtr, deltaX, deltaY);
        }

        public virtual void Scroll(Vector2 scrollDelta, Vector2 point) {

            _assertValidState();
            var deltaX = (int)(scrollDelta.x * _numberOfPixelsPerUnityUnit);
            var deltaY = (int)(scrollDelta.y * _numberOfPixelsPerUnityUnit);
            var pointerX = (int)(point.x * _nativeWidth);
            var pointerY = (int)(point.y * _nativeHeight);
            WebView_scrollAtPoint(_nativeWebViewPtr, deltaX, deltaY, pointerX, pointerY);
        }

        public virtual void SelectAll() {

            _assertValidState();
            // If the focused element is an input with a select() method, then use that.
            // Otherwise, travel up the DOM until we get to the body or a contenteditable
            // element, and then select its contents.
            ExecuteJavaScript(
                @"(function() {
                    var element = document.activeElement || document.body;
                    while (!(element === document.body || element.getAttribute('contenteditable') === 'true')) {
                        if (typeof element.select === 'function') {
                            element.select();
                            return;
                        }
                        element = element.parentElement;
                    }
                    var range = document.createRange();
                    range.selectNodeContents(element);
                    var selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(range);
                })();",
                null
            );
        }

        public void SetResolution(float pixelsPerUnityUnit) {

            // Note: this method can be called prior to initialization.
            _numberOfPixelsPerUnityUnit = pixelsPerUnityUnit;
            _resize();
        }

        public static void SetStorageEnabled(bool enabled) {

            WebView_setStorageEnabled(enabled);
        }

        public virtual void ZoomIn() {

            _assertValidState();
            WebView_zoomIn(_nativeWebViewPtr);
        }

        public virtual void ZoomOut() {

            _assertValidState();
            WebView_zoomOut(_nativeWebViewPtr);
        }

        protected IntPtr _currentViewportNativeTexture;

    #if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN
        protected const string _dllName = "VuplexWebViewWindows";
    #elif (UNITY_STANDALONE_OSX && !UNITY_EDITOR) || UNITY_EDITOR_OSX
        protected const string _dllName = "VuplexWebViewMac";
    #elif UNITY_WSA
        protected const string _dllName = "VuplexWebViewUwp";
    #elif UNITY_ANDROID
        protected const string _dllName = "VuplexWebViewAndroid";
    #else
        protected const string _dllName = "__Internal";
    #endif

        protected float _height; // in Unity units
        Material _materialForBlitting;
        protected BridgeMessageHandler _messageHandler = new BridgeMessageHandler();
        // Height in pixels.
        protected int _nativeHeight {
            get {
                // Height must be non-zero
                return Math.Max(1, (int)(_height * _numberOfPixelsPerUnityUnit));
            }
        }
        protected IntPtr _nativeWebViewPtr = IntPtr.Zero;
        // Width in pixels.
        protected int _nativeWidth {
            get {
                // Width must be non-zero
                return Math.Max(1, (int)(_width * _numberOfPixelsPerUnityUnit));
            }
        }
        protected float _numberOfPixelsPerUnityUnit = Config.NumberOfPixelsPerUnityUnit;
        List<string> _pageLoadScripts = new List<string>();
        List<Action<bool>> _pendingCanGoBackCallbacks = new List<Action<bool>>();
        List<Action<bool>> _pendingCanGoForwardCallbacks = new List<Action<bool>>();
        protected Dictionary<string, Action<string>> _pendingJavaScriptResultCallbacks = new Dictionary<string, Action<string>>();
        static readonly Regex STREAMING_ASSETS_URL_REGEX = new Regex(@"^streaming-assets:(//)?(.*)$", RegexOptions.IgnoreCase);
        const string USER_AGENT_EXCEPTION_MESSAGE = "Unable to set the User-Agent string, because a webview has already been created with the default User-Agent. On Windows and macOS, SetUserAgent() can only be called prior to creating any webviews.";
        protected Texture2D _videoTexture;
        protected bool _viewUpdatesAreEnabled = true;
        protected Texture2D _viewportTexture;
        protected float _width; // in Unity units

        protected void _assertValidState(){

            if (!IsInitialized) {
                throw new InvalidOperationException("Methods cannot be called on an uninitialized webview. Please initialize it first with IWebView.Init().");
            }

            if (IsDisposed) {
                throw new InvalidOperationException("Methods cannot be called on a disposed webview.");
            }
        }

        protected virtual Material _createMaterialForBlitting() {

            return Utils.CreateDefaultMaterial();
        }

        Texture2D _getReadableTexture() {

            // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tempRenderTexture = RenderTexture.GetTemporary(
                _nativeWidth,
                _nativeHeight,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            if (_materialForBlitting == null) {
                _materialForBlitting = _createMaterialForBlitting();
            }
            // Use the version of Graphics.Blit() that accepts a material
            // so that any transformations needed are performed with the shader.
            Graphics.Blit(Texture,tempRenderTexture,_materialForBlitting);
            // Backup the currently set RenderTexture
            RenderTexture previousRenderTexture = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tempRenderTexture;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D readableTexture = new Texture2D(_nativeWidth, _nativeHeight);
            // Copy the pixels from the RenderTexture to the new Texture
            readableTexture.ReadPixels(new Rect(0, 0, tempRenderTexture.width, tempRenderTexture.height), 0, 0);
            readableTexture.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previousRenderTexture;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tempRenderTexture);
            return readableTexture;
        }

        void _getSelectedText(Action<string> callback) {

            // window.getSelection() doesn't work on the content of <textarea> and <input> elements in
            // Gecko and legacy Edge.
            // https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection#Related_objects
            ExecuteJavaScript(
                @"var element = document.activeElement;
                if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
                    element.value.substring(element.selectionStart, element.selectionEnd);
                } else {
                    window.getSelection().toString();
                }",
                callback
            );
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleCanGoBackResult(string message) {

            var result = Boolean.Parse(message);
            var callbacks = new List<Action<bool>>(_pendingCanGoBackCallbacks);
            _pendingCanGoBackCallbacks.Clear();
            foreach (var callback in callbacks) {
                try {
                    callback(result);
                } catch (Exception e) {
                    Debug.LogError("An exception occurred while calling the callback for CanGoBack: " + e);
                }
            }
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleCanGoForwardResult(string message) {

            var result = Boolean.Parse(message);
            var callBacks = new List<Action<bool>>(_pendingCanGoForwardCallbacks);
            _pendingCanGoForwardCallbacks.Clear();
            foreach (var callBack in callBacks) {
                try {
                    callBack(result);
                } catch (Exception e) {
                    Debug.LogError("An exception occurred while calling the callForward for CanGoForward: " + e);
                }
            }
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleCloseRequested(string message) {

            var handler = CloseRequested;
            if (handler != null) {
                CloseRequested(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleJavaScriptResult(string message) {

            var components = message.Split(new char[] { ',' }, 2);
            var resultCallbackId = components[0];
            var result = components[1];
            var callback = _pendingJavaScriptResultCallbacks[resultCallbackId];
            _pendingJavaScriptResultCallbacks.Remove(resultCallbackId);
            callback(result);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleLoadFailed(string unusedParam) {

            if (PageLoadFailed != null) {
                PageLoadFailed(this, EventArgs.Empty);
            }
            var e = new ProgressChangedEventArgs(ProgressChangeType.Failed, 1.0f);
            OnLoadProgressChanged(e);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleLoadFinished(string unusedParam) {

            var e = new ProgressChangedEventArgs(ProgressChangeType.Finished, 1.0f);
            OnLoadProgressChanged(e);
            foreach (var script in _pageLoadScripts) {
                ExecuteJavaScript(script, null);
            }
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleLoadStarted(string unusedParam) {

            var e = new ProgressChangedEventArgs(ProgressChangeType.Started, 0.0f);
            OnLoadProgressChanged(e);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleLoadProgressUpdate(string progressString) {

            var progress = float.Parse(progressString);
            var e = new ProgressChangedEventArgs(ProgressChangeType.Updated, progress);
            OnLoadProgressChanged(e);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleMessageEmitted(string serializedMessage) {

            _messageHandler.HandleMessage(serializedMessage);
        }

        /// <summary>
        /// The native plugin invokes this method.
        /// </summary>
        void HandleTextureChanged(string textureString) {

            var nativeTexture = new IntPtr(Int64.Parse(textureString));
            if (nativeTexture == _currentViewportNativeTexture) {
                return;
            }
            var previousNativeTexture = _currentViewportNativeTexture;
            _currentViewportNativeTexture = nativeTexture;
            if (_viewUpdatesAreEnabled) {
                _viewportTexture.UpdateExternalTexture(_currentViewportNativeTexture);
            }

            if (previousNativeTexture != IntPtr.Zero && previousNativeTexture != _currentViewportNativeTexture) {
                WebView_destroyTexture(previousNativeTexture, SystemInfo.graphicsDeviceType.ToString());
            }
        }

        void MessageHandler_JavaScriptResultReceived(object sender, EventArgs<StringWithIdBridgeMessage> e) {

            var resultCallbackId = e.Value.id;
            var result = e.Value.value;
            var callback = _pendingJavaScriptResultCallbacks[resultCallbackId];
            _pendingJavaScriptResultCallbacks.Remove(resultCallbackId);
            callback(result);
        }

        protected void MessageHandler_UrlChanged(object sender, UrlChangedEventArgs e) {

            OnUrlChanged(e);
        }

        protected virtual void OnLoadProgressChanged(ProgressChangedEventArgs e) {

            if (LoadProgressChanged != null) {
                LoadProgressChanged(this, e);
            }
        }

        protected virtual void OnUrlChanged(UrlChangedEventArgs e) {

            if (Url == e.Url) {
                return;
            }
            Url = e.Url;
            if (UrlChanged != null) {
                UrlChanged(this, e);
            }
        }

        protected virtual void _resize() {

            // Only trigger a resize if the webview has been initialized
            if (_viewportTexture) {
                _assertValidState();
                Utils.ThrowExceptionIfAbnormallyLarge(_nativeWidth, _nativeHeight);
                WebView_resize(_nativeWebViewPtr, _nativeWidth, _nativeHeight);
            }
        }

        protected string _transformStreamingAssetsUrlIfNeeded(string originalUrl) {

            if (originalUrl == null) {
                throw new ArgumentException("URL cannot be null.");
            }
            var match = STREAMING_ASSETS_URL_REGEX.Match(originalUrl);
            if (!match.Success) {
                return originalUrl;
            }
            var urlPath = match.Groups[2].Captures[0].Value;
            return "file://" + Path.Combine(Application.streamingAssetsPath, urlPath);
        }

        static void _unitySendMessage(string gameObjectName, string methodName, string message) {

            Dispatcher.RunOnMainThread(() => {
                var gameObj = GameObject.Find(gameObjectName);
                if (gameObj == null) {
                    Debug.LogErrorFormat("Unable to send the message, because there is no GameObject named '{0}'", gameObjectName);
                    return;
                }
                gameObj.SendMessage(methodName, message);
            });
        }

        [DllImport(_dllName)]
        static extern void WebView_blur(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_canGoBack(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_canGoForward(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_clearAllData();

        [DllImport(_dllName)]
        static extern void WebView_click(IntPtr webViewPtr, int x, int y);

        [DllImport(_dllName)]
        protected static extern void WebView_destroyTexture(IntPtr texture, string graphicsApi);

        [DllImport(_dllName)]
        static extern void WebView_destroy(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_disableViewUpdates(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_enableViewUpdates(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_executeJavaScript(IntPtr webViewPtr, string javaScript, string resultCallbackId);

        [DllImport(_dllName)]
        static extern void WebView_focus(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern bool WebView_globallySetUserAgentToMobile(bool mobile);

        [DllImport(_dllName)]
        static extern bool WebView_globallySetUserAgent(string userAgent);

        [DllImport(_dllName)]
        static extern void WebView_goBack(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_goForward(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_handleKeyboardInput(IntPtr webViewPtr, string input);

        [DllImport(_dllName)]
        static extern void WebView_loadHtml(IntPtr webViewPtr, string html);

        [DllImport(_dllName)]
        static extern void WebView_loadUrl(IntPtr webViewPtr, string url);

        [DllImport(_dllName)]
        static extern void WebView_loadUrlWithHeaders(IntPtr webViewPtr, string url, string newlineDelimitedHttpHeaders);

        [DllImport(_dllName)]
        static extern void WebView_reload(IntPtr webViewPtr);

        [DllImport(_dllName)]
        protected static extern void WebView_resize(IntPtr webViewPtr, int width, int height);

        [DllImport(_dllName)]
        static extern void WebView_scroll(IntPtr webViewPtr, int deltaX, int deltaY);

        [DllImport(_dllName)]
        static extern void WebView_scrollAtPoint(IntPtr webViewPtr, int deltaX, int deltaY, int pointerX, int pointerY);

        [DllImport(_dllName)]
        static extern void WebView_setStorageEnabled(bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_zoomIn(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_zoomOut(IntPtr webViewPtr);
    }
}
#endif // UNITY_EDITOR || UNITY_STANDLONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
