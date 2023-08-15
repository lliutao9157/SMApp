using System;

namespace SMApp
{
  /// <summary>
  /// Provides the HTTP version numbers.
  /// </summary>
  public class HttpVersion
  {
    #region Public Fields

    /// <summary>
    /// Provides a <see cref="Version"/> instance for the HTTP/1.0.
    /// </summary>
    public static readonly Version Version10 = new Version (1, 0);

    /// <summary>
    /// Provides a <see cref="Version"/> instance for the HTTP/1.1.
    /// </summary>
    public static readonly Version Version11 = new Version (1, 1);

    #endregion

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpVersion"/> class.
    /// </summary>
    public HttpVersion ()
    {
    }

    #endregion
  }
}
