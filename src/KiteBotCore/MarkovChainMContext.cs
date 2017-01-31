using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KiteBotCore.Json;
using Microsoft.EntityFrameworkCore;

namespace KiteBotCore
{
    public class MarkovChainMContext : DbContext
    {
        public DbSet<MarkovMessage> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=./Content/messages.db");
        }
    }
}
