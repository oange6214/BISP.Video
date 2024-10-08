﻿namespace BISP.Video;

/// <summary>
/// Video related exception.
/// </summary>
///
/// <remarks><para>The exception is thrown in the case of some video related issues, like
/// failure of initializing codec, compression, etc.</para></remarks>
///
public class VideoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoException"/> class.
    /// </summary>
    ///
    /// <param name="message">Exception's message.</param>
    ///
    public VideoException(string message) :
        base(message)
    { }
}