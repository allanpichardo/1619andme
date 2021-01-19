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
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if VUPLEX_MRTK
    using Microsoft.MixedReality.Toolkit.Input;
#endif

namespace Vuplex.WebView {

    [HelpURL("https://developer.vuplex.com/webview/IPointerInputDetector")]
    public class CanvasPointerInputDetector : DefaultPointerInputDetector {

        Canvas _cachedCanvas;
        RectTransform _cachedRectTransform;

        protected override Vector2 _convertToNormalizedPoint(PointerEventData pointerEventData) {

            var canvas = _getCanvas();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_getRectTransform(), pointerEventData.position, camera, out localPoint);
            return _convertToNormalizedPoint(localPoint);
        }

        protected override Vector2 _convertToNormalizedPoint(Vector3 worldPosition) {

            var localPoint = _getRectTransform().InverseTransformPoint(worldPosition);
            return _convertToNormalizedPoint(localPoint);
        }

        Vector2 _convertToNormalizedPoint(Vector2 localPoint) {

            var normalizedPoint = Rect.PointToNormalized(_getRectTransform().rect, localPoint);
            normalizedPoint.y = 1 - normalizedPoint.y;
            return normalizedPoint;
        }

        Canvas _getCanvas() {

            // Note: If the instance is moved from one Canvas to another,
            //       the old Canvas will still be cached.
            if (_cachedCanvas == null) {
                _cachedCanvas = GetComponentInParent<Canvas>();
            }
            return _cachedCanvas;
        }

        RectTransform _getRectTransform() {

            if (_cachedRectTransform == null) {
                _cachedRectTransform = GetComponent<RectTransform>();
            }
            return _cachedRectTransform;
        }

        protected override bool _positionIsZero(PointerEventData eventData) {

            return eventData.position == Vector2.zero;
        }

    // Code specific to Microsoft's Mixed Reality Toolkit.
    // To enable this code, add VUPLEX_MRTK to "Scripting Define Symbols" in Player Settings.
    #if VUPLEX_MRTK
        void Start() {
            // Add a NearInteractionTouchable script to allow touch interactions
            // to trigger the IMixedRealityPointerHandler methods.
            var touchable = gameObject.AddComponent<NearInteractionTouchableUnityUI>();
            touchable.EventsToReceive = TouchableEventType.Pointer;
        }
    #endif
    }
}
