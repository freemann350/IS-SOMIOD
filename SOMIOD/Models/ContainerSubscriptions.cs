using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIOD.Models
{
    public class ContainerSubscriptions
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string creation_dt { get; set; }
        public int parent { get; set; }
        public string event_type { get; set; }
        public string endpoint { get; set; }
    }
}