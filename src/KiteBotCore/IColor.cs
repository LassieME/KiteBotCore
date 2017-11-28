namespace KiteBotCore
{
    public interface IRole
    {
        ulong Id { get; set; }
    }

    public interface IColor : IRole
    {
    }
}