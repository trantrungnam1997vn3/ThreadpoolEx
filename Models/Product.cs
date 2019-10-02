using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace TestThreadpool.Models
{
    public class Product
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public String Email { get; set; }
    }
}
