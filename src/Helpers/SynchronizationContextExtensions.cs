using System.Threading;

namespace Minimal.Mvvm;

public static class SynchronizationContextExtensions
{
    public static bool CheckAccess(this SynchronizationContext sc)
    {
        return ReferenceEquals(SynchronizationContext.Current, sc);//TODO use known contexts
    }
}
