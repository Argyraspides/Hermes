

/// <summary>
/// This interface defines a contract for classes that build URLs. The TParameters
/// type parameter must be a class that implements IQueryParameters.
/// </summary>
/// <typeparam name="TParameters">A type parameter that must be a class which implements IQueryParameters.</typeparam>
public interface IUrlBuilder<TParameters> where TParameters : IQueryParameters
{
    /// <summary>
    /// This method takes query parameters and returns a complete URL as a string
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    string BuildUrl(TParameters parameters);
}
