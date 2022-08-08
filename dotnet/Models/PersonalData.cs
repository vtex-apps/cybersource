using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class PersonalData
    {
        [JsonProperty("birthDate")]
        public string BirthDate { get; set; }

        [JsonProperty("cellPhone")]
        public string CellPhone { get; set; }

        [JsonProperty("businessPhone")]
        public string BusinessPhone { get; set; }

        [JsonProperty("fancyName")]
        public string FancyName { get; set; }

        [JsonProperty("corporateName")]
        public string CorporateName { get; set; }

        [JsonProperty("document")]
        public string Document { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("isPJ")]
        public string IsPj { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("homePhone")]
        public string HomePhone { get; set; }

        [JsonProperty("isFreeStateRegistration")]
        public string IsFreeStateRegistration { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("nickName")]
        public string NickName { get; set; }

        [JsonProperty("stateRegistration")]
        public string StateRegistration { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("customerClass")]
        public string CustomerClass { get; set; }

        [JsonProperty("createdIn")]
        public string CreatedIn { get; set; }

        [JsonProperty("businessDocument")]
        public string BusinessDocument { get; set; }
    }
}

