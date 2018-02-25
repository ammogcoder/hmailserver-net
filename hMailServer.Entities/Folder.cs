using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Entities
{
    public class Folder
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public long ParentId { get; set; }
        public string Name { get; set; }
        public bool Subscribed { get; set; }
        public DateTime CreationTime { get; set; }
        public long CurrentUid { get; set; }
    }
}
