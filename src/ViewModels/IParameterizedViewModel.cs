using System;
using System.Collections.Generic;
using System.Text;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents a ViewModel that can accept an initialization parameter.
    /// </summary>
    public interface IParameterizedViewModel
    {
        /// <summary>
        /// Gets or sets a parameter associated with the ViewModel. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Thread-affinity: the setter must be invoked on the UI thread.
        /// </para>
        /// </remarks>
        object? Parameter { get; set; }
    }
}
