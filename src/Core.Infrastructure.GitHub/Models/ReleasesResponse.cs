using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Infrastructure.GitHub.Models
{
    public class ReleasesResponse
    {
        public Release[] Data { get; set; }
    }
}
