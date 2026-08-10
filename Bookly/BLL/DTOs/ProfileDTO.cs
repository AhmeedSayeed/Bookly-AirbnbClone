using Microsoft.AspNetCore.Http;
using System;

namespace BLL.DTOs.Account
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public string ProfilePhotoUrl { get; set; }

        public IFormFile? ProfilePhoto { get; set; }

        public bool IsHost { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}