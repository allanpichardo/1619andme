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
using UnityEditor;

namespace Vuplex.WebView {

    public static class DocumentationLinkDrawer {

        /// <summary>
        /// Draws a "View documentation" link.
        /// </summary>
        public static void DrawDocumentationLink(string url) {

            var linkStyle = new GUIStyle {
                richText = true,
                padding = new RectOffset {
                    top = 10,
                    bottom = 10
                }
            };
            var learnMoreLinkClicked = GUILayout.Button(
                EditorUtils.TextWithColor("View documentation", EditorUtils.GetLinkColor()),
                linkStyle
            );
            var linkRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
            // Unity's editor GUI doesn't support underlines, so fake it.
            GUI.Label(
                linkRect,
                EditorUtils.TextWithColor("_______________________", EditorUtils.GetLinkColor()),
                new GUIStyle {
                    richText = true,
                    padding = new RectOffset {
                        top = 12,
                        bottom = 10
                }
            });

            if (learnMoreLinkClicked) {
                Application.OpenURL(url);
            }
        }
    }
}
