using Godot;


/// <summary>
/// Represents an abstract base class for managing resources with unique hash identification.
/// This class provides core functionality for resource handling, including hash generation
/// and resource data management.
/// </summary>
public abstract class Resource
{

    public Resource()
    {
    }

    // Tracks whether a valid hash has been generated for this resource
    private bool m_hashGenerated = false;
    private string m_hash;


    /// <summary>
    /// Gets the unique hash identifier for this resource. If the resource cannot be uniquely
    /// identified due to insufficient information, a warning will be logged and an empty string
    /// will be returned.
    /// </summary>
    /// <value>
    /// A string representing the unique hash of the resource. Returns an empty string if the
    /// resource cannot be properly hashed.
    /// </value>
    /// <remarks>
    /// The hash is generated only once. If the resource doesn't contain sufficient
    /// information for hashing, a warning will be displayed through Godot's warning system.
    /// </remarks>
    public string Hash
    {
        get
        {
            if (!m_hashGenerated)
            {
                GD.PushWarning("Resource: " + this + " cannot be uniquely identified as it doesn't contain the minimum amount of information for identification");
            }
            return m_hash;
        }
        private set => m_hash = value;
    }


    public byte[] ResourceData { get; protected set; }
    public string ResourcePath { get; protected set; }

    /// <summary>
    /// Generates and caches the hash value for the resource if it meets the requirements
    /// for being hashable.
    /// </summary>
    protected void GenerateHash()
    {
        if (IsHashable())
        {
            Hash = GenerateHashCore();
            m_hashGenerated = true;
        }
        else
        {
            Hash = string.Empty;
            m_hashGenerated = false;
        }
    }

    /// <summary>
    /// When implemented in a derived class, generates a unique hash value for the resource.
    /// </summary>
    /// <returns>
    /// A string representing the unique hash value for the resource.
    /// </returns>
    /// <remarks>
    /// Implementing classes should ensure that the generated hash is unique and consistent
    /// for the same resource content.
    /// </remarks>
    public abstract string GenerateHashCore();

    /// <summary>
    /// When implemented in a derived class, determines whether the resource contains
    /// sufficient information to generate a unique hash.
    /// </summary>
    /// <returns>
    /// true if the resource can be hashed; otherwise, false.
    /// </returns>
    /// <remarks>
    /// Implementing classes should define the specific conditions that must be met
    /// for a resource to be considered hashable.
    /// </remarks>
    public abstract bool IsHashable();
}
