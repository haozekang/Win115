using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Entities
{
    public class TokenEntity
    {
        public int Id { get; set; }
        public string? AccessToken { get; set; }
        public DateTime? AccessExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresIn { get; set; }
    }
}
