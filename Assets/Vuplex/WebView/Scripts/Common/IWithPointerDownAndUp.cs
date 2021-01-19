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

namespace Vuplex.WebView {
    /// <summary>
    /// An interface implemented by a webview if it supports `PointerDown`
    /// and `PointerUp`, which can be used to implement
    /// drag-and-drop interactions
    /// </summary>
    public interface IWithPointerDownAndUp {
        /// <summary>
        /// Dispatches a "mouse down" click event.
        /// This can be used in conjunction with `IWithMovablePointer.MovePointer` and
        /// `PointerUp` to implement drag-and-drop interactions.
        /// </summary>
        /// <param name="point">
        /// The x and y components of the point are values
        /// between 0 and 1 that are normalized to the width and height, respectively. For example,
        /// `point.x = x in Unity units / width in Unity units`.
        /// Like in the browser, the origin is in the upper-left corner,
        /// the positive direction of the y-axis is down, and the positive
        /// direction of the x-axis is right.
        /// </param>
        /// <seealso cref="IWebView.Click"/>
        void PointerDown(Vector2 point);

        /// <summary>
        /// Like `PointerDown(Vector2)`, except it also accepts a
        /// `PointerOptions` parameter to modify the behavior
        /// (e.g. to trigger a right click or a double click).
        /// </summary>
        void PointerDown(Vector2 point, PointerOptions options);

        /// <summary>
        /// Dispatches a mouse up click event.
        /// This can be used in conjunction with `PointerDown` and
        /// `IWithMovablePointer.MovePointer` and to implement drag-and-drop interactions.
        /// </summary>
        /// <param name="point">
        /// The x and y components of the point are values
        /// between 0 and 1 that are normalized to the width and height, respectively. For example,
        /// `point.x = x in Unity units / width in Unity units`.
        /// Like in the browser, the origin is in the upper-left corner,
        /// the positive direction of the y-axis is down, and the positive
        /// direction of the x-axis is right.
        /// </param>
        /// <seealso cref="Click"/>
        void PointerUp(Vector2 point);

        /// <summary>
        /// Like `PointerUp(Vector2)`, except it also accepts a
        /// `PointerOptions` parameter to modify the behavior
        /// (e.g. to trigger a right click or a double click).
        /// </summary>
        void PointerUp(Vector2 point, PointerOptions options);
    }
}
