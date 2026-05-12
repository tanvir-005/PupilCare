using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Models;
using System.Threading.Tasks;

namespace PupilCare.Components
{
    public class SystemInfoViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public SystemInfoViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string part)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSetting();
            }

            return View(part, settings);
        }
    }
}
