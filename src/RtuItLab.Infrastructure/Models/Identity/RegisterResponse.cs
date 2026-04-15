using System.Collections.Generic;

namespace RtuItLab.Infrastructure.Models.Identity
{
    public class RegisterResponse
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
