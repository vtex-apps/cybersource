using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class ValidatedUser
    {
        public string AuthStatus { get; set; }
        public string Id { get; set; }
        public string User { get; set; }    // email
    }
}