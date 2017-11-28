using System;
using System.Collections.Generic;
using KiteBotCore.Json;

namespace KiteBotCore
{
    public interface IRankRole : IRole
    {
        TimeSpan RequiredTimeSpan { get; set; }

        List<Color> Colors { get; set; }
    }
}