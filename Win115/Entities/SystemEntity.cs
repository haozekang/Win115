using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Entities
{
    public class SystemEntity
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
    }
}
