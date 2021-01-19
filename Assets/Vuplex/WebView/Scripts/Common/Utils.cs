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
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {

    /// <summary>
    /// Static utility methods used internally by 3D WebView.
    /// </summary>
    public static class Utils {

        public static byte[] ConvertAndroidByteArray(AndroidJavaObject arrayObject) {

            // Unity 2019.1 and newer logs a warning that converting from byte[] is obsolete
            // but older versions are incapable of converting from sbyte[].
            #if UNITY_2019_1_OR_NEWER
                return (byte[])(Array)AndroidJNIHelper.ConvertFromJNIArray<sbyte[]>(arrayObject.GetRawObject());
            #else
                return AndroidJNIHelper.ConvertFromJNIArray<byte[]>(arrayObject.GetRawObject());
            #endif
        }

        public static Material CreateDefaultMaterial() {

            // Construct a new material, because Resources.Load<T>() returns a singleton.
            return new Material(Resources.Load<Material>("DefaultViewportMaterial"));
        }

        public static string GetGraphicsApiErrorMessage(GraphicsDeviceType activeGraphicsApi, GraphicsDeviceType[] acceptableGraphicsApis) {

            var isValid = Array.IndexOf(acceptableGraphicsApis, activeGraphicsApi) != -1;
            if (isValid) {
                return null;
            }
            var acceptableApiStrings = acceptableGraphicsApis.ToList().Select(api => api.ToString());
            var acceptableApisList = String.Join(" or ", acceptableApiStrings.ToArray());
            return String.Format("Unsupported graphics API: Vuplex 3D WebView requires {0} for this platform, but the selected graphics API is {1}. Please go to Player Settings and set \"Graphics APIs\" to {0}.", acceptableApisList, activeGraphicsApi);
        }

        public static void ThrowExceptionIfAbnormallyLarge(int width, int height) {

            // Anything over 14.7 megapixels (5k) is almost certainly a mistake.
            if (width * height > 14700000) {
                throw new ArgumentException(String.Format("The application specified an abnormally large webview size ({0}px x {1}px), and webviews of this size are normally only created by mistake. A webview's default resolution is 1300px per Unity unit, so it's likely that you specified a large physical size by mistake or need to adjust the resolution. For more information, please see IWebView.SetResolution: https://developer.vuplex.com/webview/IWebView#SetResolution", width, height));
            }
        }
    }
}
