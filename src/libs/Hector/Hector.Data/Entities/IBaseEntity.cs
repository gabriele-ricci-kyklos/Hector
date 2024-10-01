namespace Hector.Data.Entities
{
    public interface IBaseEntity
    {
        public string TableName { get; }
        public string Alias { get; }
        public bool IsView { get; }
    }
}
