using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Account
{
    public class BecomeAHostViewModel
    {
        [Required(ErrorMessage = "Please upload a valid ID document.")]
        public IFormFile IdDocument { get; set; }
    }
}