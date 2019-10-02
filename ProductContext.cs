using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestThreadpool.Models;

namespace TestThreadpool
{
    class ProductContext : DbContext
    {

        public ProductContext() : base("name=test")
        {
        }

        public DbSet<Product> Products { get; set; }

    }
}
