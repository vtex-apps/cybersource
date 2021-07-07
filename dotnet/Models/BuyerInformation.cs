using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BuyerInformation
    {
        public string merchantCustomerId { get; set; }
        public string dateOfBirth { get; set; }
        public string vatRegistrationNumber { get; set; }
        public string companyTaxId { get; set; }
        public List<PersonalIdentification> personalIdentification { get; set; }
        public string hashedPassword { get; set; }
        public string mobilePhone { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class PersonalIdentification
    {
        public string type { get; set; }
        public string id { get; set; }
        public string issuedBy { get; set; }
    }
}