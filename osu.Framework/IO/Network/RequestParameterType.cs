// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.IO.Network
{
    /// <summary>
    /// Determines the type of a key-value parameter supplied to a <see cref="WebRequest"/>.
    /// </summary>
    public enum RequestParameterType
    {
        /// <summary>
        /// This parameter should be contained in the query string of the request URL.
        /// </summary>
        Query,

        /// <summary>
        /// This parameter should be placed in the body of the request, using the <c>multipart/form-data</c> MIME type.
        /// </summary>
        Form
    }
}
