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
            // On simule un vrai navigateur (Chrome) pour que Virtual DJ nous traite comme la page web
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _baseUrl = $"http://{ip}:{port}";
        }

        public async Task EnvoyerCommandeAsync(string commande)
        {
            if (string.IsNullOrWhiteSpace(commande)) return;

            try
            {
                // CORRECTION : On remplace UNIQUEMENT les espaces. 
                // Ne plus utiliser EscapeDataString pour préserver les " et les \ du chemin Windows
                string script = commande.Replace(" ", "%20");

                string url = $"{_baseUrl}/execute?script={script}";

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