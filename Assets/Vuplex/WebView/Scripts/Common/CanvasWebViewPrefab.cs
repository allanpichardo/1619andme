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
using System;
using UnityEngine;
using UnityEngine.UI;
#if NET_4_6 || NET_STANDARD_2_0
    using System.Threading.Tasks;
#endif

namespace Vuplex.WebView {
    /// <summary>
    /// A prefab that makes it easy to create and interact with an `IWebView` in a Canvas.
    /// </summary>
    /// <remarks>
    /// `CanvasWebViewPrefab` takes care of creating and initializing an `IWebView`, displaying its texture,
    /// and handling click and scroll interactions from the user. So, all you need to do is load some web
    /// content from a URL or HTML string, and then the user can view and interact with it.
    ///
    /// You can create a `CanvasWebViewPrefab` one of the following ways:
    /// - By dragging CanvasWebViewPrefab.prefab into a Canvas via the editor and setting its "Initial URL" property.
    /// - By instantiating an instance at runtime with `CanvasWebViewPrefab.Instantiate()` and then
    ///   waiting for its `Initialized` event, after which you can call methods on its `WebView` property.
    ///
    /// `CanvasWebViewPrefab` handles standard events from Unity's input system
    /// (like `IPointerDownHandler`), so it works with input modules that extend Unity's `BaseInputModule`,
    /// like Unity's Standalone Input Module and third-party modules.
    ///
    /// If your use case requires a high degree of customization, you can instead create an `IWebView`
    /// outside of the prefab with `Web.CreateWebView()`.
    /// </remarks>
    [HelpURL("https://developer.vuplex.com/webview/CanvasWebViewPrefab")]
    public class CanvasWebViewPrefab : MonoBehaviour {

        /// <summary>
        /// Indicates that the prefab was clicked. Note that the prefab automatically
        /// calls the `IWebView.Click()` method for you.
        /// </summary>
        public event EventHandler<ClickedEventArgs> Clicked {
            add {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Clicked += value;
            }
            remove {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Clicked -= value;
            }
        }

        /// <summary>
        /// Indicates that the prefab finished initializing,
        /// so its `WebView` property is ready to use.
        /// </summary>
        public EventHandler Initialized;

        /// <summary>
        /// Indicates that the prefab was scrolled. Note that the prefab automatically
        /// calls the `IWebView.Scroll()` method for you.
        /// </summary>
        public event EventHandler<ScrolledEventArgs> Scrolled {
            add {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Scrolled += value;
            }
            remove {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Scrolled -= value;
            }
        }

        /// <summary>
        /// If you drag a CanvasWebViewPrefab.prefab into the scene via the editor,
        /// you can set this property in the editor to make it so that the instance
        /// automatically initializes itself with the given URL. To load a new URL
        /// after the prefab has been initialized, use `IWebView.LoadUrl()` instead.
        /// </summary>
        [Label("Initial URL to Load (Optional)")]
        [Tooltip("Or you can leave the Initial URL blank if you want to initialize the prefab programmatically by calling Init().")]
        public string InitialUrl;

        /// <summary>
        /// Sets the webview's initial resolution in pixels per Unity unit.
        /// You can change the resolution to make web content appear larger or smaller.
        /// For more information on scaling web content, see
        /// [this support article](https://support.vuplex.com/articles/how-to-scale-web-content).
        /// </summary>
        [Label("Initial Resolution (px / Unity unit)")]
        [Tooltip("You can change this to make web content appear larger or smaller.")]
        public float InitialResolution = 1;

        /// <summary>
        /// Determines how the prefab handles drag interactions.
        /// </summary>
        [Tooltip("Note: \"Drag Within Page\" is not supported on iOS or UWP.")]
        public DragMode DragMode = DragMode.DragToScroll;

        [Header("Other Settings")]
        /// <summary>
        /// Clicking is enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// </summary>
        public bool ClickingEnabled = true;

        /// <summary>
        /// Hover interactions are enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// Note that hovering only works for webview implementations that
        /// support the `IWithMovablePointer` interface (i.e. Android, Windows, and macOS).
        /// </summary>
        [Tooltip("Note: Hovering is not supported on iOS or UWP.")]
        public bool HoveringEnabled = true;

        /// <summary>
        /// Scrolling is enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// </summary>
        public bool ScrollingEnabled = true;

        /// <summary>
        /// Determines the threshold (in web pixels) for triggering a drag. The default is 20.
        /// </summary>
        /// <remarks>
        /// When the `DragMode` is set to `DragToScroll`, this property determines
        /// the distance that the pointer must drag before it's no longer
        /// considered a click.
        /// </remarks>
        /// <remarks>
        /// When the `DragMode` is set to `DragWithinPage`, this property determines
        /// the distance that the pointer must drag before it triggers
        /// a drag within the page.
        /// </remarks>
        [Label("Drag Threshold (px)")]
        [Tooltip("Determines the threshold (in web pixels) for triggering a drag.")]
        public float DragThreshold = 20;

        [Obsolete("The CanvasWebViewPrefab.DragToScrollThreshold property is obsolete. Please use DragThreshold instead.")]
        public float DragToScrollThreshold { get; set; }

        /// <summary>
        /// Allows the scroll sensitivity to be adjusted.
        /// The default sensitivity is 15.
        /// </summary>
        public float ScrollingSensitivity = 15;

        /// <summary>
        /// The prefab's material.
        /// </summary>
        public Material Material {
            get {
                return _webViewPrefab == null ? null : _webViewPrefab.Material;
            }
            set {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Material = value;
            }
        }

        /// <summary>
        /// Controls whether the instance is visible or hidden.
        /// </summary>
        public bool Visible {
            get {
                if (_webViewPrefab == null) {
                    return true;
                }
                return _webViewPrefab.Visible;
            }
            set {
                _instantiateWebViewPrefabIfNeeded();
                _webViewPrefab.Visible = value;
            }
        }

        /// <summary>
        /// A reference to the prefab's `IWebView` instance, which
        /// is available after the `Initialized` event is raised.
        /// Before initialization is complete, this property is `null`.
        /// </summary>
        public IWebView WebView {
            get {
                return _webViewPrefab == null ? null : _webViewPrefab.WebView;
            }
        }

        /// <summary>
        /// Destroys the instance and its children. Note that you don't have
        /// to call this method if you destroy the instance's parent with
        /// `Object.Destroy()`.
        /// </summary>
        public void Destroy() {

            UnityEngine.Object.Destroy(gameObject);
        }

        /// <summary>
        /// Creates a new instance and initializes it asynchronously.
        /// </summary>
        /// <remarks>
        /// The `WebView` property is available after initialization completes,
        /// which is indicated by the `Initialized` event.
        /// </remarks>
        /// <example>
        /// Example:
        ///
        /// ```
        /// var canvas = GameObject.Find("Canvas");
        /// canvasWebViewPrefab = CanvasWebViewPrefab.Instantiate();
        /// canvasWebViewPrefab.transform.parent = canvas.transform;
        /// var rectTransform = canvasWebViewPrefab.transform as RectTransform;
        /// rectTransform.anchoredPosition3D = Vector3.zero;
        /// rectTransform.offsetMin = Vector2.zero;
        /// rectTransform.offsetMax = Vector2.zero;
        /// canvasWebViewPrefab.transform.localScale = Vector3.one;
        /// canvasWebViewPrefab.Initialized += (sender, e) => {
        ///     canvasWebViewPrefab.WebView.LoadUrl("https://vuplex.com");
        /// };
        /// ```
        /// </example>
        public static CanvasWebViewPrefab Instantiate() {

            return Instantiate(new WebViewOptions());
        }

        /// <summary>
        /// Like `Instantiate()`, except it also accepts an object
        /// of options flags that can be used to alter the generated webview's behavior.
        /// </summary>
        public static CanvasWebViewPrefab Instantiate(WebViewOptions options) {

            var prefabPrototype = (GameObject) Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject) Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab.Init(options);
            return canvasWebViewPrefab;
        }

        /// <summary>
        /// Like `Instantiate()`, except it initializes the instance with an existing, initialized
        /// `IWebView` instance. This causes the `CanvasWebViewPrefab` to use the existing
        /// `IWebView` instance instead of creating a new one.
        /// </summary>
        public static CanvasWebViewPrefab Instantiate(IWebView webView) {

            var prefabPrototype = (GameObject) Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject) Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab.Init(webView);
            return canvasWebViewPrefab;
        }

        /// <summary>
        /// Asynchronously initializes the instance.
        /// </summary>
        /// <remarks>
        /// You only need to call this method if you place a CanvasWebViewPrefab.prefab in your
        /// scene via the Unity editor but don't set its "Initial URL" property.
        /// You don't need to call this method if you set the "Initial URL" property in
        /// the editor or if you instantiate the prefab programmatically at runtime using
        /// `Instantiate()`. In those cases, this method is called automatically for you.
        /// This method asynchronously initializes the `WebView` property, which is
        /// available for use after the `Initialized` event is raised.
        /// </remarks>
        public void Init() {

            Init(new WebViewOptions());
        }

        /// <summary>
        /// Like `Init()`, except it also accepts an object
        /// of options flags that can be used to alter the webview's behavior.
        /// </summary>
        public void Init(WebViewOptions options) {

            _init(options);
        }

        /// <summary>
        /// Like `Init()`, except it initializes the instance with an existing, initialized
        /// `IWebView` instance. This causes the `CanvasWebViewPrefab` to use the existing
        /// `IWebView` instance instead of creating a new one.
        /// </summary>
        public void Init(IWebView webView) {

            _init(new WebViewOptions(), webView);
        }

        /// <summary>
        /// By default, CanvasWebViewPrefab detects pointer input events like clicks through
        /// Unity's event system, but you can use this method to override the way that
        /// input events are detected.
        /// </summary>
        public void SetPointerInputDetector(IPointerInputDetector pointerInputDetector) {

            _instantiateWebViewPrefabIfNeeded();
            _setCustomPointerInputDetector = true;
            _webViewPrefab.SetPointerInputDetector(pointerInputDetector);
        }

    #if NET_4_6 || NET_STANDARD_2_0
        /// <summary>
        /// Returns a task that resolves when the prefab is initialized
        /// (i.e. when its `WebView` property is ready for use).
        /// </summary>
        public Task WaitUntilInitialized() {

            var taskCompletionSource = new TaskCompletionSource<bool>();
            var isInitialized = _webViewPrefab != null && _webViewPrefab.WebView != null;
            if (isInitialized) {
                taskCompletionSource.SetResult(true);
            } else {
                Initialized += (sender, e) => taskCompletionSource.SetResult(true);
            }
            return taskCompletionSource.Task;
        }
    #endif

        bool _setCustomPointerInputDetector;
        WebViewPrefab _webViewPrefab;

        void Awake() {

            if (!String.IsNullOrEmpty(InitialUrl)) {
                Init();
            }
        }

        Rect _getRect() {

            return GetComponent<RectTransform>().rect;
        }

        void _init(WebViewOptions options, IWebView initializedWebView = null) {

            _instantiateWebViewPrefabIfNeeded();
            _webViewPrefab.Initialized += WebViewPrefab_Initialized;
            _webViewPrefab.transform.SetParent(transform, false);
            _webViewPrefab.transform.localPosition = new Vector3(0, _getRect().height / 2, 0);
            _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
            if (!_setCustomPointerInputDetector) {
                var inputDetector = GetComponent<IPointerInputDetector>();
                if (inputDetector == null) {
                    Debug.LogWarning("The CanvasWebViewPrefab instance has no CanvasPointerInputDetector, so pointer events will not be detected.");
                } else {
                    _webViewPrefab.SetPointerInputDetector(inputDetector);
                }
            }
            _webViewPrefab.Collider.enabled = false;

            if (initializedWebView == null) {
                // Init with an initial size of 1 x 1 to prevent a huge initial size.
                _webViewPrefab.Init(1, 1, options);
            } else {
                _webViewPrefab.Init(initializedWebView);
            }
            _webViewPrefab.Visible = true;
        }


        void _instantiateWebViewPrefabIfNeeded() {

            if (_webViewPrefab == null) {
                var prefabPrototype = (GameObject) Resources.Load("WebViewPrefab");
                var viewGameObject = (GameObject) Instantiate(prefabPrototype);
                _webViewPrefab = viewGameObject.GetComponent<WebViewPrefab>();
                // Keep it invisible until Init() is called.
                _webViewPrefab.Visible = false;
                _webViewPrefab.SetView(GetComponent<ViewportMaterialView>());
            }
        }

        void _resizeWebViewPrefabIfNeeded() {

            if (_webViewPrefab == null || _webViewPrefab.WebView == null) {
                return;
            }
            var rect = _getRect();
            var size = _webViewPrefab.WebView.Size;
            if (!(rect.width == size.x && rect.height == size.y)) {
                _webViewPrefab.Resize(rect.width, rect.height);
            }
            var expectedPosition = new Vector3(0, rect.height / 2, 0);
            if (_webViewPrefab.transform.localPosition != expectedPosition) {
                _webViewPrefab.transform.localPosition = expectedPosition;
            }
        }

        void Update() {

            _resizeWebViewPrefabIfNeeded();
            _updateFieldsIfNeeded();
        }

        void _updateFieldsIfNeeded() {

            if (_webViewPrefab == null) {
                return;
            }
            if (_webViewPrefab.ClickingEnabled != ClickingEnabled) {
                _webViewPrefab.ClickingEnabled = ClickingEnabled;
            }
            if (_webViewPrefab.DragMode != DragMode) {
                _webViewPrefab.DragMode = DragMode;
            }
            if (_webViewPrefab.DragThreshold != DragThreshold) {
                _webViewPrefab.DragThreshold = DragThreshold;
            }
            if (_webViewPrefab.HoveringEnabled != HoveringEnabled) {
                _webViewPrefab.HoveringEnabled = HoveringEnabled;
            }
            if (_webViewPrefab.ScrollingEnabled != ScrollingEnabled) {
                _webViewPrefab.ScrollingEnabled = ScrollingEnabled;
            }
            if (_webViewPrefab.ScrollingSensitivity != ScrollingSensitivity) {
                _webViewPrefab.ScrollingSensitivity = ScrollingSensitivity;
            }
        }

        void WebViewPrefab_Initialized(object sender, EventArgs e) {

            var resolution = InitialResolution;
            if (InitialResolution <= 0) {
                Debug.LogWarningFormat("Invalid value for CanvasWebViewPrefab.InitialResolution: {0}. The resolution will instead default to 1.", InitialResolution);
                resolution = 1;
            }
            _webViewPrefab.WebView.SetResolution(resolution);
            // Resize to the actual desired size.
            var rect = _getRect();
            _webViewPrefab.Resize(rect.width, rect.height);

            var handler = Initialized;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }

            var initialUrl = InitialUrl;
            if (!String.IsNullOrEmpty(initialUrl)) {
                var url = initialUrl.Trim();
                if (!url.Contains(":")) {
                    url = "http://" + url;
                }
                _webViewPrefab.WebView.LoadUrl(url);
            }

            #if UNITY_IOS && !UNITY_EDITOR
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                    Debug.LogWarning("iOSWebView doesn't support video inside a Canvas with Render Mode 'Screen Space - Overlay'. If you need video support, please use 'Screen Space - Camera' instead.");
                }
            #endif
        }
    }
}
