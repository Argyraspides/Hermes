public class Resource
{
    public string Hash { get; protected set; }
    public byte[] ResourceData { get; protected set; }
    public string ResourcePath { get; protected set; }
    public bool IsExternalResource { get; protected set; }
    public virtual void GenerateHash() { Hash = ""; }
}
