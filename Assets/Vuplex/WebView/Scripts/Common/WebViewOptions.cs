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
namespace Vuplex.WebView {

    /// <summary>
    /// Options that can be passed to the `WebViewPrefab.Instantiate()` to alter the behavior of
    /// the WebView created.
    /// </summary>
    public struct WebViewOptions {

        /// <summary>
        /// If set to `true`, makes it so that clicking on the webview doesn't
        /// automatically focus that webview.
        /// </summary>
        public bool clickWithoutStealingFocus;

        /// <summary>
        /// On iOS devices and on Android devices that don't support native video rendering,
        /// the iOS and Android plugins normally allocate extra resources for rendering
        /// videos. This field can be used to optionally disable video support in those
        /// cases so that extra resources aren't allocated.
        /// </summary>
        public bool disableVideo;

        /// <summary>
        /// 3D WebView automatically selects which native plugin to use based on
        /// the build platform and which plugins are installed in the project.
        /// However, if you have multiple plugins installed for a single platform,
        /// this option can be used to specify which plugin to use in order to override
        /// the default behavior.
        /// </summary>
        /// <remarks>
        /// Currently, Android is the only platform that supports multiple 3D WebView
        /// plugins: `WebPluginType.Android` and `WebPluginType.AndroidGecko`. If both
        /// plugins are installed in the same project, `WebPluginType.AndroidGecko` will be used by default.
        /// However, you can override this to force `WebPluginType.Android` to be used instead by specifying
        /// `preferredPlugins = new WebPluginType[] { WebPluginType.Android }`.
        /// </remarks>
        public WebPluginType[] preferredPlugins;
    }
}
