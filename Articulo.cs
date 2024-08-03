using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto1_Optimizacion
{
    public class Articulo
    {
        public string ISBN { get; set; }
        public string name { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public decimal? Price { get; set; }
        public int? quantity { get; set; }
    }

}
