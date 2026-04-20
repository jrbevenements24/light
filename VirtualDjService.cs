using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyShowController
{
    public class VirtualDjService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VirtualDjService(string ip = "127.0.0.1", int port = 80)
        {
            _httpClient = new HttpClient();
            _baseUrl = $"http://{ip}:{port}";
        }

        public async Task EnvoyerCommandeAsync(string commande)
        {
            if (string.IsNullOrWhiteSpace(commande)) return;

            try
            {
                // CORRECTION : Le chemin exact attendu par Virtual DJ est /execute?script=
                string url = $"{_baseUrl}/execute?script={Uri.EscapeDataString(commande)}";

                HttpResponseMessage reponse = await _httpClient.GetAsync(url);

                if (reponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[VDJ OK] : {commande}");
                }
                else
                {
                    Console.WriteLine($"[Erreur VDJ] Code : {reponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur VDJ] Impossible de joindre Virtual DJ : {ex.Message}");
            }
        }
    }
}