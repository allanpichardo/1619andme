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

namespace Vuplex.WebView {

    /// <summary>
    /// Event args to indicate that a file download was requested.
    /// </summary>
    public class DownloadRequestedEventArgs : EventArgs {

        public DownloadRequestedEventArgs(string url, string fileName, string contentType) {
            Url = url;
            FileName = fileName;
            ContentType = contentType;
        }

        /// <summary>
        /// The URL of the file to download.
        /// </summary>
        readonly public string Url;

        /// <summary>
        /// The file name indicated by the `Content-Disposition` response header
        /// or `null` if no file name was specified.
        /// </summary>
        readonly public string FileName;

        /// <summary>
        /// The mime type indicated by the `Content-Type` response header,
        /// or `null` if no content type was specified.
        /// </summary>
        readonly public string ContentType;
    }
}

