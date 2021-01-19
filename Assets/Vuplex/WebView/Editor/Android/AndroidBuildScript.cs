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
#if UNITY_ANDROID
#pragma warning disable CS0618
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView {
    /// <summary>
    /// Pre-build script that validates the project's Graphics API settings.
    /// </summary>
    public class AndroidBuildScript : IPreprocessBuild {

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.Android) {
                return;
            }
            _validateGraphicsApi();
            EditorUtils.AssertThatOculusLowOverheadModeIsDisabled();
            _warnIfAndroidManifestIsNeeded();
        }

        static void _validateGraphicsApi() {

            var autoGraphicsApiEnabled = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            if (autoGraphicsApiEnabled) {
                throw new BuildFailedException("Graphics settings error: Vuplex 3D WebView for Android requires that \"Auto Graphics API\" be disabled in order to ensure that OpenGLES3 or OpenGLES2 is used. Please go to Player Settings, disable \"Auto Graphics API\", and set \"Graphics APIs\" to OpenGLES3 or OpenGLES2.");
            }
            var selectedGraphicsApi = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0];
            var error = Utils.GetGraphicsApiErrorMessage(selectedGraphicsApi, new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLES2 });
            if (error != null) {
                throw new BuildFailedException(error);
            }
        }

        /// <summary>
        /// Detects and warns if cleartext traffic will be blocked.
        /// https://support.vuplex.com/articles/how-to-enable-cleartext-traffic-on-android
        /// </summary>
        static void _warnIfAndroidManifestIsNeeded() {

            var targetSdkVersion = PlayerSettings.Android.targetSdkVersion;
            var targetSdkIsAffected = targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto || (int)targetSdkVersion >= 28;
            if (!targetSdkIsAffected) {
                return;
            }
            var androidManifestPath = EditorUtils.PathCombine(new string[] { Application.dataPath, "Plugins", "Android", "AndroidManifest.xml" });
            if (File.Exists(androidManifestPath)) {
                return;
            }
            Debug.LogWarning("The application's Target API Level is set to 28 or higher, which means that Android will block requests to plain http:// (non-https) URLs by default. For instructions on how to enable plain http:// URLs, visit https://support.vuplex.com/articles/how-to-enable-cleartext-traffic-on-android .");
        }
    }
}
#endif
