namespace Xandernate.Entities
{
    public abstract class DaoBase : DaoBase<int>
    {
        public DaoBase()
        {
        }

        public DaoBase(int id)
            : base(id)
        {
        }
    }

    public abstract class DaoBase<TPrimaryKey> : IDao<TPrimaryKey>
    {
        public TPrimaryKey Id { get; set; }

        public DaoBase()
        {
        }

        public DaoBase(TPrimaryKey id)
        {
            Id = id;
        }
    }
}
