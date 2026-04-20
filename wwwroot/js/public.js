const API_URL = '/api/show';
const container = document.getElementById('scenario-container');
const countdownEl = document.getElementById('countdown');

let lastDesignSignature = "";

document.addEventListener('DOMContentLoaded', () => {
    chargerScenarios();
    setInterval(checkAndApplyDesign, 500);
    setInterval(verifierEtat, 1000);
    setInterval(sendHeartbeat, 2000);
});

async function sendHeartbeat() { try { await fetch(`${API_URL}/heartbeat`, { method: 'POST' }); } catch (e) { } }

// --- DESIGN ---
async function checkAndApplyDesign() {
    try {
        const res = await fetch(`${API_URL}/design`);
        if (!res.ok) return;

        const d = await res.json();
        const currentSignature = JSON.stringify(d);
        if (currentSignature === lastDesignSignature) return;
        lastDesignSignature = currentSignature;

        const pVal = d.pageBgValue || '#121212';
        const bNVal = d.btnNormalValue || '#00ff88';
        const bPVal = d.btnPressedValue || '#00cc66';
        const txt = d.btnTextColor || 'black';
        const shape = d.btnShape || '10px';
        const font = d.btnFont || 'Arial, sans-serif';
        const shadowCol = d.btnShadowColor || '#000000';
        const glowCol = d.btnGlowColor || '#00ff88';

        if (d.pageBgType === 'image') {
            document.body.style.background = `url('${pVal}') no-repeat center center fixed`;
            document.body.style.backgroundSize = 'cover';
        } else {
            document.body.style.background = pVal;
            document.body.style.backgroundImage = 'none';
        }

        const getBg = (t, v) => (t === 'image') ? `background-image:url('${v}') !important; background-size:cover !important; background-color:transparent !important;` : `background-color:${v} !important; background-image:none !important;`;
        const normCss = getBg(d.btnNormalType, bNVal);
        const pressCss = getBg(d.btnPressedType, bPVal);
        let shadowCss = "box-shadow: none;";
        let borderCss = "border: 2px solid rgba(255,255,255,0.2);";
        if (d.btnShadow === 'soft') shadowCss = `box-shadow: 0 4px 6px ${shadowCol};`;
        if (d.btnShadow === 'hard') { shadowCss = `box-shadow: 4px 4px 0px ${shadowCol};`; borderCss = `border: 2px solid ${shadowCol};`; }
        if (d.btnShadow === 'neon') { shadowCss = `box-shadow: 0 0 15px ${glowCol};`; borderCss = `border: 1px solid ${glowCol};`; }

        let styleTag = document.getElementById('dynamic-design-style');
        if (!styleTag) { styleTag = document.createElement('style'); styleTag.id = 'dynamic-design-style'; document.head.appendChild(styleTag); }
        let fontFaceCss = "";
        if (d.customFontFiles && d.customFontFiles.length > 0) { d.customFontFiles.forEach(file => { const family = file.split('.')[0]; fontFaceCss += `@font-face { font-family: '${family}'; src: url('fonts/${file}'); }\n`; }); }

        styleTag.innerHTML = `${fontFaceCss} .btn-scenario { ${normCss} color: ${txt} !important; border-radius: ${shape} !important; font-family: ${font} !important; ${borderCss} transition: transform 0.1s, background-color 0.1s; ${shadowCss} font-weight: bold; text-transform: uppercase; cursor: pointer; display: block; width: 100%; margin-bottom: 10px; padding: 15px; } .btn-scenario:active { ${pressCss} transform: scale(0.95); filter: brightness(0.9); } .btn-scenario:hover { filter: brightness(1.1); z-index: 10; } .btn-scenario.disabled { opacity: 0.5; pointer-events: none; }`;
    } catch (e) { }
}

async function chargerScenarios() {
    try {
        const response = await fetch(`${API_URL}/list`);
        if (!response.ok) throw new Error("Erreur serveur");
        const scenarios = await response.json();
        const messageChargement = container.querySelector('p');
        if (messageChargement || container.children.length !== scenarios.length) {
            container.innerHTML = '';
            if (scenarios.length === 0) { container.innerHTML = '<p style="color:#888; text-align:center;">Aucun scénario disponible.</p>'; return; }
            scenarios.forEach(scenar => { const btn = document.createElement('button'); btn.className = 'btn-scenario'; const vraiNom = scenar.name || scenar.Name || "Scénario Sans Nom"; btn.innerText = vraiNom; btn.onclick = () => lancerScenario(vraiNom); container.appendChild(btn); });
        }
    } catch (error) { container.innerHTML = `<p style="color:red; text-align:center;">ERREUR CHARGEMENT</p>`; }
}

async function lancerScenario(nom) {
    if (!nom) return;
    afficherRebours(3);
    await fetch(`${API_URL}/play/${encodeURIComponent(nom)}`, { method: 'POST' });
}

function afficherRebours(secondes) {
    if (!countdownEl) return;
    countdownEl.style.display = 'block';
    countdownEl.innerText = secondes;
    let counter = secondes;
    const interval = setInterval(() => {
        counter--;
        if (counter > 0) { countdownEl.innerText = counter; }
        else { countdownEl.innerText = "GO !"; setTimeout(() => { countdownEl.style.display = 'none'; }, 1000); clearInterval(interval); }
    }, 1000);
}

// --- ETAT (Urgence / Fin de Soirée / Playing) ---
async function verifierEtat() {
    try {
        const res = await fetch(`${API_URL}/status`);
        const data = await res.json();
        const boutons = document.querySelectorAll('.btn-scenario');
        let overlay = document.getElementById('statusOverlay');

        // MODE URGENCE
        if (data.emergency) {
            if (!overlay) { overlay = document.createElement('div'); overlay.id = 'statusOverlay'; document.body.appendChild(overlay); }
            overlay.style = "position:fixed; top:0; left:0; width:100%; height:100%; background:black; color:red; display:flex; justify-content:center; align-items:center; flex-direction:column; z-index:9999;";
            overlay.innerHTML = "<h1 style='font-size:3em;'>⚠️ ARRÊT D'URGENCE</h1><p style='color:white;'>Attendez la fin de l'intervention.</p>";
        }
        // MODE FIN DE SOIRÉE (Ce que tu as demandé)
        else if (data.endOfNight) {
            if (!overlay) { overlay = document.createElement('div'); overlay.id = 'statusOverlay'; document.body.appendChild(overlay); }
            overlay.style = "position:fixed; top:0; left:0; width:100%; height:100%; background:#110033; color:#00d2ff; display:flex; justify-content:center; align-items:center; flex-direction:column; z-index:9999;";
            overlay.innerHTML = "<h1 style='font-size:3em;'>🌙 SOIRÉE TERMINÉE</h1><p style='color:white; font-size:1.5em;'>L'animation est fermée. Merci et bonne nuit !</p>";
        }
        else {
            if (overlay) overlay.remove();
        }

        if (data.playing) boutons.forEach(b => b.classList.add('disabled'));
        else boutons.forEach(b => b.classList.remove('disabled'));
    } catch (e) { }
}