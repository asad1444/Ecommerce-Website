using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Mvc;

namespace loginlogout.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext context;

        public ReviewController(ApplicationDbContext context)
        {
            this.context = context;
        }

       
    }
}

