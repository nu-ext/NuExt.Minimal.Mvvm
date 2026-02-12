using System;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents a ViewModel that participates in a parent-child relationship within a logical ViewModel tree.
    /// </summary>
    public interface IParentedViewModel
    {

        /// <summary>
        /// Gets or sets the parent ViewModel. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Re-parenting is supported. Implementations must prevent self-assignment and cycles;
        /// when such an assignment is attempted, they may throw <see cref="InvalidOperationException"/>.
        /// </para>
        /// <para>
        /// Thread-affinity: the setter must be invoked on the UI thread.
        /// </para>
        /// </remarks>
        object? ParentViewModel { get; set; }
    }
}
