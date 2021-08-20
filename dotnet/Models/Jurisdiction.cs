using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class Jurisdiction
    {
        public string country { get; set; }
        public string code { get; set; }
        public string taxable { get; set; }
        public string rate { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string region { get; set; }
        public string taxAmount { get; set; }
        public string taxName { get; set; }
    }
}
