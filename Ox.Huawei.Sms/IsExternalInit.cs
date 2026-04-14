using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Dummy class for .NET Standard to handle init accessors, works when compiled.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class IsExternalInit
	{
	}
}
#pragma warning restore IDE0130 // Namespace does not match folder structure
