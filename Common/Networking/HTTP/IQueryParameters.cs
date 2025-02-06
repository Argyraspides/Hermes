using System.Collections.Generic;


/// <summary>
/// This interface defines the contract for any class that wants to represent query parameters
/// for a URL. By implementing this interface, a class promises to be able to convert its
/// properties into a dictionary of string key-value pairs that can be used in a URL.
/// </summary>
public interface IQueryParameters
{

    /// <summary>
    /// This method converts the implementing class's properties into a dictionary
    /// where both keys and values are strings. This is useful for URL construction
    /// since query parameters are always string-based.
    /// </summary>
    /// <returns></returns>
    IDictionary<string, string> ToQueryDictionary();

}
