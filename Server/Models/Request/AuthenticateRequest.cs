using System.ComponentModel.DataAnnotations;

namespace Server.Models.Request
{
    public class AuthenticateRequest
    {
        [Required]
        public string Key { get; set; }
    }

}
