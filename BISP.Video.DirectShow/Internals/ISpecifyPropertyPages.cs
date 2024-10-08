﻿namespace BISP.Video.DirectShow.Internals;

/// <summary>
/// The interface indicates that an object supports property pages.
/// </summary>
///
[ComImport,
Guid("B196B28B-BAB4-101A-B69C-00AA00341D07"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpecifyPropertyPages
{
    /// <summary>
    /// Fills a counted array of GUID values where each GUID specifies the
    /// CLSID of each property page that can be displayed in the property
    /// sheet for this object.
    /// </summary>
    ///
    /// <param name="pPages">Pointer to a CAUUID structure that must be initialized
    /// and filled before returning.</param>
    ///
    /// <returns>Return's <b>HRESULT</b> error code.</returns>
    ///
    [PreserveSig]
    int GetPages(out CAUUID pPages);
}