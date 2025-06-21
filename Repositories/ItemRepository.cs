using HighThroughputApi.Interfaces;
using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HighThroughputApi.Repositories
{
    public class ItemRepository : BaseRepository<Item>, IItemRepository
    {
        public ItemRepository(AppDbContext context) : base(context) { }

    }
}
