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
using System.Collections.Generic;
using UnityEngine;

namespace Vuplex.WebView.Demos {

    /// <summary>
    /// Script that detects keys pressed on the hardware
    /// (USB or Bluetooth) keyboard and emits the corresponding
    /// strings that can be passed to `IWebView.HandleKeyboardInput()`
    /// or `IWithKeyDownAndUp.KeyDown()` and `KeyUp()`.
    /// </summary>
    class HardwareKeyboardListener : MonoBehaviour {

        [Obsolete("The HardwareKeyboardListener.InputReceived event is now deprecated. Please use the HardwareKeyboardListener.KeyDownReceived event instead.")]
        public event EventHandler<KeyboardInputEventArgs> InputReceived {
            add {
                KeyDownReceived += value;
            }
            remove {
                KeyDownReceived -= value;
            }
        }

        public event EventHandler<KeyboardInputEventArgs> KeyDownReceived;

        public event EventHandler<KeyboardInputEventArgs> KeyUpReceived;

        public static HardwareKeyboardListener Instantiate() {

            return (HardwareKeyboardListener) new GameObject("HardwareKeyboardListener").AddComponent<HardwareKeyboardListener>();
        }

        List<string> _keysDown = new List<string>();

        static readonly string[] _keyValues = new string[] {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "`", "-", "=", "[", "]", "\\", ";", "'", ",", ".", "/", " ", "Enter", "Backspace", "Tab", "ArrowUp", "ArrowDown", "ArrowRight", "ArrowLeft"
        };

        // Keys that don't show up correctly in Input.inputString.
        static readonly string[] _keyValuesUndetectableThroughInputString = new string[] {
            "Tab", "ArrowUp", "ArrowDown", "ArrowRight", "ArrowLeft"
        };

        bool _areKeysUndetectableThroughInputStringPressed() {

            foreach (var key in _keyValuesUndetectableThroughInputString) {
                // Use GetKey instead of GetKeyDown because on macOS, Input.inputString
                // contains garbage when the arrow keys are held down.
                if (Input.GetKey(_getUnityKeyNameForJsKeyValue(key))) {
                    return true;
                }
            }
            return false;
        }

        KeyModifier _getModifiers() {

            var modifiers = KeyModifier.None;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                modifiers |= KeyModifier.Shift;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                modifiers |= KeyModifier.Control;
            }
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                modifiers |= KeyModifier.Alt;
            }
            if (Input.GetKey(KeyCode.LeftWindows) ||
                Input.GetKey(KeyCode.RightWindows)) {
                modifiers |= KeyModifier.Meta;
            }
            // Don't pay attention to the command keys on Windows because Unity has a bug
            // where it falsly reports the command keys are pressed after switching languages
            // with the windows+space shortcut.
            #if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                if (Input.GetKey(KeyCode.LeftCommand) ||
                    Input.GetKey(KeyCode.RightCommand)) {
                    modifiers |= KeyModifier.Meta;
                }
            #endif

            return modifiers;
        }

        string _getUnityKeyNameForJsKeyValue(string keyValue) {

            switch (keyValue) {
                case " ":
                    return "space";
                case "ArrowUp":
                    return "up";
                case "ArrowDown":
                    return "down";
                case "ArrowRight":
                    return "right";
                case "ArrowLeft":
                    return "left";
            }
            return keyValue.ToLower();
        }

        void _processKeysReleased(KeyModifier modifiers) {

            if (_keysDown.Count == 0) {
                return;
            }
            var keysDownCopy = new List<string>(_keysDown);
            foreach (var key in keysDownCopy) {
                bool keyUp = false;
                try {
                    keyUp = Input.GetKeyUp(_getUnityKeyNameForJsKeyValue(key));
                } catch (ArgumentException ex) {
                    Debug.LogError("Invalid key value passed to Input.GetKeyUp: " + ex);
                    _keysDown.Remove(key);
                    return;
                }
                if (keyUp) {
                    var handler = KeyUpReceived;
                    if (handler != null) {
                        handler(this, new KeyboardInputEventArgs(key, modifiers));
                    }
                    _keysDown.Remove(key);
                }
            }
        }

        void _processKeysPressed(KeyModifier modifiers) {

            if (!(Input.anyKeyDown || Input.inputString.Length > 0)) {
                return;
            }
            var handler = KeyDownReceived;
            var modifierKeysPressed = !(modifiers == KeyModifier.None || modifiers == KeyModifier.Shift);
            var keysUndetectableThroughInputStringArePressed = _areKeysUndetectableThroughInputStringPressed();
            // On Windows, when modifier keys are held down, Input.inputString is blank
            // even if other keys are pressed. So, use Input.GetKeyDown() in that scenario.
            if (modifierKeysPressed || keysUndetectableThroughInputStringArePressed) {
                foreach (var key in _keyValues) {
                    if (Input.GetKeyDown(_getUnityKeyNameForJsKeyValue(key))) {
                        if (handler != null) {
                            handler(this, new KeyboardInputEventArgs(key, modifiers));
                        }
                        _keysDown.Add(key);
                    }
                }
            } else {
                // Using Input.inputString when possible is preferable since it
                // handles different languages and characters that would be hard
                // to support using Input.GetKeyDown().
                foreach (var character in Input.inputString) {
                    string characterString;
                    switch (character) {
                        case '\b':
                            characterString = "Backspace";
                            break;
                        case '\n':
                        case '\r':
                            characterString = "Enter";
                            break;
                        case (char)0xF728:
                            // 0xF728 = NSDeleteFunctionKey on macOS
                            characterString = "Delete";
                            break;
                        default:
                            characterString = character.ToString();
                            break;
                    }
                    if (handler != null) {
                        handler(this, new KeyboardInputEventArgs(characterString, modifiers));
                    }
                    _keysDown.Add(characterString);
                }
            }
        }

        void Update() {

            var modifiers = _getModifiers();
            _processKeysPressed(modifiers);
            _processKeysReleased(modifiers);
        }
    }
}
