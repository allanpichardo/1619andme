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

#if NET_4_6 || NET_STANDARD_2_0
    using System.Threading.Tasks;
#endif

namespace Vuplex.WebView {

    /// <summary>
    /// `Web` is the top-level static class for the 3D WebView plugin.
    /// It contains static methods for configuring the module and creating resources.
    /// </summary>
    public static class Web {

        /// <summary>
        /// Clears all data that persists between webview instances,
        /// including cookies, storage, and cached resources.
        /// </summary>
        /// <remarks>
        /// On Windows and macOS, this method can only be called prior to
        /// creating any webviews.
        /// </remarks>
        public static void ClearAllData() {

            _pluginFactory.GetPlugin().ClearAllData();
        }

    #if NET_4_6 || NET_STANDARD_2_0
        /// <summary>
        /// Creates a material and texture that a webview can use for rendering.
        /// </summary>
        /// <remarks>
        /// Note that `WebViewPrefab` and `CanvasWebViewPrefab` take care of material creation for you, so you only need
        /// to call this method directly if you need to create an `IWebView` instance outside of a prefab with
        /// `Web.CreateWebView()`.
        /// </remarks>
        public static Task<Material> CreateMaterial() {

            var task = new TaskCompletionSource<Material>();
            CreateMaterial(task.SetResult);
            return task.Task;
        }
    #endif

        /// <summary>
        /// Like the other version of `CreateMaterial()`, except it uses a callback
        /// instead of a `Task` in order to be compatible with legacy .NET.
        /// </summary>
        public static void CreateMaterial(Action<Material> callback) {

            _pluginFactory.GetPlugin().CreateMaterial(callback);
        }

        /// <summary>
        /// Like `CreateMaterial`, except it creates a material that a webview
        /// can use for rendering video. If the platform doesn't need a separate
        /// material and texture for video, this method returns `null`.
        /// </summary>
        /// <remarks>
        /// Currently, iOS is the only platform that always uses a separate texture
        /// for video. Android only uses a separate video texture on versions of Android
        /// older than 6.0. For other platforms, video content is always integrated into
        /// the main texture.
        /// </remarks>
        public static void CreateVideoMaterial(Action<Material> callback) {

            _pluginFactory.GetPlugin().CreateVideoMaterial(callback);
        }

    #if NET_4_6 || NET_STANDARD_2_0
        /// <summary>
        /// Creates a special texture that a webview can use for rendering, using the given
        /// width and height in Unity units (not pixels). The webview plugin automatically
        /// resizes a texture when it initializes or resizes a webview, so in practice, you
        /// can simply use the dimensions of 1x1 like `CreateTexture(1, 1, callback)`.
        /// </summary>
        /// <remarks>
        /// Note that the `WebViewPrefab` takes care of texture creation for you, so you only need
        /// to call this method directly if you need to create an `IWebView` instance outside of a prefab with
        /// `Web.CreateWebView()`.
        /// </remarks>
        public static Task<Texture2D> CreateTexture(float width, float height) {

            var task = new TaskCompletionSource<Texture2D>();
            CreateTexture(width, height, task.SetResult);
            return task.Task;
        }
    #endif

        /// <summary>
        /// Like the other version of `CreateTexture()`, except it uses a callback
        /// instead of a `Task` in order to be compatible with legacy .NET.
        /// </summary>
        public static void CreateTexture(float width, float height, Action<Texture2D> callback) {

            _pluginFactory.GetPlugin().CreateTexture(width, height, callback);
        }

        /// <summary>
        /// Creates a new webview in a platform-agnostic way. After a webview
        /// is created, it must be initialized by calling one of its `Init()`
        /// methods.
        /// </summary>
        /// <remarks>
        /// Note that `WebViewPrefab` takes care of creating and managing
        /// an `IWebView` instance for you, so you only need to call this method directly
        /// if you need to create an `IWebView` instance outside of a prefab
        /// (for example, to connect it to your own custom GameObject).
        /// </remarks>
        /// <example>
        /// <![CDATA[
        /// ```cs
        /// Web.CreateMaterial(material => {
        ///     // Set the material attached to this GameObject so that it can display the web content.
        ///     GetComponent<Renderer>().material = material;
        ///     var webView = Web.CreateWebView();
        ///     webView.Init(material.mainTexture, 1, 1);
        ///     webView.LoadUrl("https://vuplex.com");
        /// });
        /// ```
        /// ]]>
        /// </example>
        public static IWebView CreateWebView() {

            return _pluginFactory.GetPlugin().CreateWebView();
        }

        /// <summary>
        /// Like `CreateWebView()`, except an array of preferred plugin types can be
        /// provided to override which 3D WebView plugin is used in the case where
        /// multiple plugins are installed for the same build platform.
        /// </summary>
        /// <remarks>
        /// Currently, Android is the only platform that supports multiple 3D WebView
        /// plugins: `WebPluginType.Android` and `WebPluginType.AndroidGecko`. If both
        /// plugins are installed in the same project, `WebPluginType.AndroidGecko` will be used by default.
        /// However, you can override this to force `WebPluginType.Android` to be used instead by passing
        /// `new WebPluginType[] { WebPluginType.Android }`.
        /// </remarks>
        public static IWebView CreateWebView(WebPluginType[] preferredPlugins) {

            return _pluginFactory.GetPlugin(preferredPlugins).CreateWebView();
        }

        /// <summary>
        /// By default, browsers block https URLs with invalid SSL certificates
        /// from being loaded. However, this method can be used to ignore
        /// certificate errors.
        /// </summary>
        /// <remarks>
        /// This method works for every package except for 3D WebView for UWP.
        /// For UWP, certificates must be whitelisted in Package.appxmanifest/Declarations/Certificates:
        /// https://www.suchan.cz/2015/10/displaying-https-page-with-invalid-certificate-in-uwp-webview/
        /// </remarks>
        public static void SetIgnoreCertificateErrors(bool ignore) {

            _pluginFactory.GetPlugin().SetIgnoreCertificateErrors(ignore);
        }

        /// <summary>
        /// Enables support for showing the native Android or iOS touch screen
        /// keyboard when an input is focused.
        /// </summary>
        /// <remarks>
        /// This method should be called prior to initializing the desired webviews.
        /// This functionality is currently only supported by 3D WebView for Android
        /// and 3D WebView for iOS. For other packages, such as
        /// 3D WebView for Android with Gecko Engine, please see Unity's `TouchScreenKeyboard` class.
        /// </remarks>
        public static void SetTouchScreenKeyboardEnabled(bool enabled) {

            var pluginWithTouchScreenKeyboard = _pluginFactory.GetPlugin() as IPluginWithTouchScreenKeyboard;
            if (pluginWithTouchScreenKeyboard != null) {
                pluginWithTouchScreenKeyboard.SetTouchScreenKeyboardEnabled(enabled);
            }
        }

        /// <summary>
        /// Controls whether data like cookies, localStorage, and cached resources
        /// is persisted between webview instances. The default is `true`, but this
        /// can be set to `false` to achieve an "incognito mode".
        /// </summary>
        /// <remarks>
        /// On Windows and macOS, this method can only be called prior to
        /// creating any webviews.
        /// </remarks>
        public static void SetStorageEnabled(bool enabled) {

            _pluginFactory.GetPlugin().SetStorageEnabled(enabled);
        }

        /// <summary>
        /// By default, webviews use a User-Agent that looks that of a desktop
        /// computer so that servers return the desktop versions of websites.
        /// If you instead want the mobile versions of websites, you can invoke
        /// this method with `true` to use the User-Agent for a mobile device.
        /// </summary>
        /// <remarks>
        /// On Windows and macOS, this method can only be called prior to
        /// creating any webviews.
        /// </remarks>
        public static void SetUserAgent(bool mobile) {

            _pluginFactory.GetPlugin().SetUserAgent(mobile);
        }

        /// <summary>
        /// Configures the module to use a custom User-Agent string.
        /// </summary>
        /// <remarks>
        /// On Windows and macOS, this method can only be called prior to
        /// creating any webviews.
        /// </remarks>
        public static void SetUserAgent(string userAgent) {

            _pluginFactory.GetPlugin().SetUserAgent(userAgent);
        }

        static internal void SetPluginFactory(WebPluginFactory pluginFactory) {

            _pluginFactory = pluginFactory;
        }

        static WebPluginFactory _pluginFactory = new WebPluginFactory();
    }
}
