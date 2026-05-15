using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MyShowController.Controllers
{
    public class MidiReq { public int Note { get; set; } }

    [Route("api/[controller]")]
    [ApiController]
    public class ShowController : ControllerBase
    {
        private readonly ShowManager _manager;

        public ShowController(ShowManager manager) { _manager = manager; }

        // --- RECHERCHE IP POUR ACCÈS MOBILE (Route : /api/show/ip) ---
        [HttpGet("ip")]
        public IActionResult GetNetworkInfo()
        {
            string bestIP = "127.0.0.1";
            try
            {
                foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var props = netInterface.GetIPProperties();
                        foreach (var addr in props.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                string ip = addr.Address.ToString();
                                if (ip.StartsWith("192.168.")) return Ok(new { ip = ip });
                                if (ip.StartsWith("10.")) bestIP = ip;
                                if (bestIP == "127.0.0.1") bestIP = ip;
                            }
                        }
                    }
                }
            }
            catch { }
            return Ok(new { ip = bestIP });
        }

        [HttpGet("dashboard")]
        public IActionResult GetDashboard()
        {
            var scenarios = _manager.LoadScenarios();
            var totalClicks = scenarios.Sum(s => s.UsageCount);
            var ranking = scenarios.OrderByDescending(s => s.UsageCount)
                                   .Select(s => new { name = s.Name, count = s.UsageCount })
                                   .ToList();

            return Ok(new
            {
                sunliteConnected = _manager.IsSunliteConnected,
                publicConnected = _manager.IsPublicPageOnline(),
                totalClicks = totalClicks,
                ranking = ranking
            });
        }

        [HttpPost("resetstats")] public IActionResult ResetStats() { _manager.ResetAllStats(); return Ok(); }
        [HttpPost("endofnight")] public IActionResult ToggleEndOfNight() { _manager.ToggleEndOfNight(); return Ok(new { active = _manager.IsEndOfNight }); }
        [HttpGet("list")] public IActionResult GetList() => Ok(_manager.LoadScenarios());
        [HttpPost("create")] public IActionResult Create([FromBody] Scenario s) { _manager.SaveScenario(s); return Ok(); }
        [HttpDelete("delete/{name}")] public IActionResult Delete([FromRoute] string name) { if (string.IsNullOrWhiteSpace(name)) return BadRequest("Nom requis"); _manager.DeleteScenario(WebUtility.UrlDecode(name)); return Ok(); }

        [HttpPost("play/{id}")]
        public IActionResult Play(string id)
        {
            if (_manager.IsEndOfNight) return StatusCode(403, "Fin de soirée active");
            if (_manager.IsPlaying) return Conflict();
            _ = _manager.PlayScenarioAsync(id);
            return Ok();
        }

        [HttpPost("stop")] public IActionResult Stop() { _manager.RequestStop(); return Ok(); }
        [HttpGet("status")] public IActionResult Status() => Ok(new { playing = _manager.IsPlaying, emergency = _manager.IsEmergency, endOfNight = _manager.IsEndOfNight });
        [HttpGet("design")] public IActionResult GetDesign() => Ok(_manager.GetDesign());
        [HttpPost("design")] public IActionResult SaveDesign([FromBody] DesignSettings d) { _manager.SaveDesign(d); return Ok(); }

        [HttpPost("upload-font")]
        public async Task<IActionResult> UploadFont(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Fichier vide");
            var fontsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts");
            if (!Directory.Exists(fontsPath)) Directory.CreateDirectory(fontsPath);
            var fileName = Path.GetFileName(file.FileName).Replace(" ", "_");
            using (var stream = new FileStream(Path.Combine(fontsPath, fileName), FileMode.Create)) await file.CopyToAsync(stream);
            return Ok(new { fileName = fileName });
        }

        [HttpPost("testmidi")] public IActionResult TestMidi([FromBody] MidiReq req) { _manager.TestMidi(req.Note, 127); return Ok(); }
        [HttpPost("heartbeat")] public IActionResult Heartbeat() { _manager.ReceiveHeartbeat(); return Ok(); }

        // --- ROUTES MACROS VIRTUAL DJ ---
        [HttpGet("macros")] public IActionResult GetMacros() => Ok(_manager.LoadMacros());
        [HttpPost("macro")] public IActionResult SaveMacro([FromBody] VdjMacro m) { _manager.SaveMacro(m); return Ok(); }
        [HttpDelete("macro/{name}")] public IActionResult DeleteMacro([FromRoute] string name) { if (string.IsNullOrWhiteSpace(name)) return BadRequest("Nom requis"); _manager.DeleteMacro(WebUtility.UrlDecode(name)); return Ok(); }
    }
}