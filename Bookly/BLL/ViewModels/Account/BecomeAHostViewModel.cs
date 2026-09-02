using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Account
{
    public class BecomeAHostViewModel
    {
        [Required(ErrorMessage = "IdDocumentRequired")]
        public IFormFile IdDocument { get; set; }
    }
}