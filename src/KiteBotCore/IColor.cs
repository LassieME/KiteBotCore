using System;

namespace KiteBotCore
{
    public interface IColor
    {
        ulong Id { get; set; }

        DateTimeOffset? RemovalAt { get; set; }
    }
}