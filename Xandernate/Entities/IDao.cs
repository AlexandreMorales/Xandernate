namespace Xandernate.Entities
{
    public interface IDao : IDao<int>
    {
    }

    public interface IDao<TPrimaryKey>
    {
        TPrimaryKey Id { get; set; }
    }
}
