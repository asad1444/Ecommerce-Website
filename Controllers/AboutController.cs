using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
public AboutController(ApplicationDbContext context,IWebHostEnvironment env)
        {
_context = context;
            _env = env;
        }
        [HttpGet]
        public IActionResult AddAbout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAbout(AboutVM about)
        {
            string filename = "";
            string myfilename = "";
            if(about.UpperPhoto != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "AboutImages");
                myfilename = about.UpperPhoto.FileName;
                string filepath = Path.Combine(folder,filename);
                string filepath2 = Path.Combine(folder, myfilename);
                about.UpperPhoto.CopyTo(new FileStream(filepath2, FileMode.Create));


                About ab = new About()
                {
                    UpperImage = myfilename,
                    Uppertitle = about.Uppertitle,
                    uppercontent = about.uppercontent,

                };

                _context.About.Add(ab);
                _context.SaveChanges();

return RedirectToAction("ShowAbout");


            }
            return View();
        }

        public IActionResult ShowAbout()
        {
            var show = _context.About.ToList();
            return View(show);
        }

        [HttpGet]
        public IActionResult DeleteAbout(int id)

        { 
            var findabout = _context.About.Find(id);
            if(findabout == null)
            {
                return NotFound();
            }
            return View(findabout);
        }

        [HttpPost,ActionName("DeleteAbout")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAbouts(int id)
        {
            var findabout = _context.About.Find(id);
            if(findabout != null)
            {
                _context.About.Remove(findabout);
                _context.SaveChanges();
                return RedirectToAction("ShowAbout");
            }
            return View(findabout);
        }

        [HttpGet]
        public IActionResult EditAbout(int id)
        {
            var editabout = _context.About.Find(id);
            if(editabout == null)
            {
                return NotFound();
            }

            AboutVM ab = new AboutVM()
            {
                AboutId = editabout.AboutId,
                Uppertitle = editabout.Uppertitle,

                uppercontent = editabout.uppercontent,

            };
            ViewBag.BannerImage = editabout.UpperImage;
            return View(ab);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAbout(AboutVM about)
        {
            var ab = _context.About.FirstOrDefault(a=> a.AboutId == about.AboutId);
            if(ab == null)
            {
                return NotFound();
            }
            if(about.UpperPhoto != null)
            {
                string folder = Path.Combine(_env.WebRootPath,"AboutImages");
                string filename2 = about.UpperPhoto.FileName;
                string filepath2 = Path.Combine(folder,filename2);

                about.UpperPhoto.CopyTo(new FileStream(filepath2, FileMode.Create));
                ab.UpperImage = filename2;

            }
            ab.AboutId = about.AboutId;
            ab.Uppertitle = about.Uppertitle;
            ab.uppercontent = about.uppercontent;
            
            _context.About.Update(ab);
            _context.SaveChanges();
            return RedirectToAction("ShowAbout");

        }
        

        
    }
}
