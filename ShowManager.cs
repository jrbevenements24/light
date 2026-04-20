using NAudio.Midi;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace MyShowController
{
    public class ShowManager
    {
        private MidiOut _midiOut;
        private bool _isPlaying = false;
        private VirtualDjService _vdjService = new VirtualDjService();
        private readonly object _fileLock = new object(); // Sécurité pour l'écriture

        public bool IsEmergency { get; private set; } = false;
        public bool IsEndOfNight { get; private set; } = false;
        public bool IsPlaying => _isPlaying;

        private CancellationTokenSource _cts;

        private const string SCENARIO_FILE = "scenarios.json";
        private const string DESIGN_FILE = "design.json";
        private const string MACROS_FILE = "macros.json";

        private TcpClient _os2lClient;
        private NetworkStream _os2lStream;
        private const int SUNLITE_PORT = 8010;
        private const string SUNLITE_IP = "127.0.0.1";
        private DateTime _lastPublicHeartbeat = DateTime.MinValue;

        public ShowManager()
        {
            // Initialisation MIDI
            int deviceId = -1;
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                var info = MidiOut.DeviceInfo(i);
                if (info.ProductName.ToLower().Contains("loopmidi")) { deviceId = i; break; }
                if (!info.ProductName.ToLower().Contains("wavetable")) deviceId = i;
            }
            if (deviceId != -1) try { _midiOut = new MidiOut(deviceId); } catch { }

            // Création des fichiers si absents
            lock (_fileLock)
            {
                if (!File.Exists(SCENARIO_FILE)) File.WriteAllText(SCENARIO_FILE, "[]");
                if (!File.Exists(DESIGN_FILE)) File.WriteAllText(DESIGN_FILE, "{}");
                if (!File.Exists(MACROS_FILE)) File.WriteAllText(MACROS_FILE, "[]");
            }

            Task.Run(async () => { while (true) { CheckAndReconnectSunlite(); await Task.Delay(2000); } });
        }

        private void CheckAndReconnectSunlite()
        {
            try
            {
                if (_os2lClient == null || !_os2lClient.Connected)
                {
                    _os2lClient?.Close();
                    _os2lClient = new TcpClient();
                    _os2lClient.Connect(SUNLITE_IP, SUNLITE_PORT);
                    _os2lStream = _os2lClient.GetStream();
                }
            }
            catch { _os2lClient = null; }
        }

        private void SendOs2l(string name)
        {
            if (_os2lClient != null && _os2lClient.Connected)
            {
                try
                {
                    var msg = new { evt = "btn", name = name, page = "", state = "on" };
                    byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg) + "\n");
                    _os2lStream.Write(data, 0, data.Length);
                }
                catch { _os2lClient = null; }
            }
        }

        public void TestMidi(int note, int velocity)
        {
            if (_midiOut == null) return;
            try
            {
                _midiOut.Send(MidiMessage.StartNote(note, velocity, 1).RawData);
                Task.Delay(100).ContinueWith(t => { try { _midiOut.Send(MidiMessage.StopNote(note, 0, 1).RawData); } catch { } });
            }
            catch { }
        }

        public void ReceiveHeartbeat() => _lastPublicHeartbeat = DateTime.Now;
        public bool IsPublicPageOnline() => (DateTime.Now - _lastPublicHeartbeat).TotalSeconds < 5;

        public DesignSettings GetDesign()
        {
            lock (_fileLock) { return JsonConvert.DeserializeObject<DesignSettings>(File.ReadAllText(DESIGN_FILE)) ?? new DesignSettings(); }
        }

        public void SaveDesign(DesignSettings design)
        {
            lock (_fileLock) { File.WriteAllText(DESIGN_FILE, JsonConvert.SerializeObject(design, Formatting.Indented)); }
        }

        public List<Scenario> LoadScenarios()
        {
            lock (_fileLock) { return JsonConvert.DeserializeObject<List<Scenario>>(File.ReadAllText(SCENARIO_FILE)) ?? new List<Scenario>(); }
        }

        public void SaveScenario(Scenario s)
        {
            lock (_fileLock)
            {
                var list = LoadScenarios();
                var index = list.FindIndex(x => x.Name.ToLower() == s.Name.ToLower());
                if (index != -1)
                {
                    s.UsageCount = list[index].UsageCount;
                    list[index] = s;
                }
                else
                {
                    list.Add(s);
                }
                File.WriteAllText(SCENARIO_FILE, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
        }

        public void DeleteScenario(string name)
        {
            lock (_fileLock)
            {
                var list = LoadScenarios();
                list.RemoveAll(x => x.Name.ToLower() == name.ToLower());
                File.WriteAllText(SCENARIO_FILE, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
        }

        public void IncrementScenarioUsage(string name)
        {
            lock (_fileLock)
            {
                var list = LoadScenarios();
                var s = list.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
                if (s != null) { s.UsageCount++; File.WriteAllText(SCENARIO_FILE, JsonConvert.SerializeObject(list, Formatting.Indented)); }
            }
        }

        public void ResetAllStats()
        {
            lock (_fileLock)
            {
                var list = LoadScenarios();
                foreach (var s in list) s.UsageCount = 0;
                File.WriteAllText(SCENARIO_FILE, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
        }

        public void ToggleEndOfNight() { IsEndOfNight = !IsEndOfNight; if (!IsEndOfNight) IsEmergency = false; }

        public List<VdjMacro> LoadMacros()
        {
            lock (_fileLock) { return JsonConvert.DeserializeObject<List<VdjMacro>>(File.ReadAllText(MACROS_FILE)) ?? new List<VdjMacro>(); }
        }

        public void SaveMacro(VdjMacro m)
        {
            lock (_fileLock)
            {
                var list = LoadMacros();
                list.RemoveAll(x => x.Name.ToLower() == m.Name.ToLower());
                list.Add(m);
                File.WriteAllText(MACROS_FILE, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
        }

        public void DeleteMacro(string name)
        {
            lock (_fileLock)
            {
                var list = LoadMacros();
                list.RemoveAll(x => x.Name.ToLower() == name.ToLower());
                File.WriteAllText(MACROS_FILE, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
        }

        public async Task PlayScenarioAsync(string scenarioName)
        {
            if (_isPlaying || IsEndOfNight) return;

            var scenar = LoadScenarios().FirstOrDefault(s => s.Name.ToLower() == scenarioName.ToLower());
            if (scenar == null) return;

            IncrementScenarioUsage(scenarioName);
            IsEmergency = false;
            _isPlaying = true;
            _cts = new CancellationTokenSource();

            try
            {
                foreach (var action in scenar.Actions)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    // --- ÉTAPE 1 : EXECUTION IMMEDIATE ---
                    if (action.Type == "os2l") SendOs2l(action.CommandName);
                    else if (action.Type == "midi") TestMidi(action.Note, action.Velocity);
                    else if (action.Type == "vdj") await _vdjService.EnvoyerCommandeAsync(action.CommandName);

                    // --- ÉTAPE 2 : PAUSE APRÈS L'ACTION ---
                    if (action.DelayMs > 0)
                    {
                        await Task.Delay(action.DelayMs, _cts.Token);
                    }
                }
            }
            catch { }
            finally { _isPlaying = false; }
        }

        public void RequestStop()
        {
            IsEmergency = true;
            _cts?.Cancel();
            SendOs2l("BLACKOUT");
            _ = _vdjService.EnvoyerCommandeAsync("stop");
        }
    }
}