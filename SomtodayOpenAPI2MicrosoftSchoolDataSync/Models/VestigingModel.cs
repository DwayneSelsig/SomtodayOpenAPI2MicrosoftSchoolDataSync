using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Models
{
    internal class VestigingModel
    {
        public Vestiging Vestiging { get; set; }        
        public List<Lesgroep> Lesgroepen { get; set; }
        public List<Medewerker> Medewerkers { get; set; }
        public List<Leerling> Leerlingen { get; set; }
        public List<OuderVerzorger> OuderVerzorgers { get; set; }        
    }
}
