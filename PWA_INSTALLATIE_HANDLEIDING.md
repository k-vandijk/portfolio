# PWA Installatie Handleiding - Ticker API Dashboard

Deze handleiding helpt je om het Ticker API Dashboard als Progressive Web App (PWA) op je telefoon te installeren.

## ✅ Wat is er geïmplementeerd?

Het dashboard is nu volledig voorbereid om als PWA te werken. De volgende functionaliteiten zijn toegevoegd:

1. **Web App Manifest** (`manifest.json`) - Bevat metadata over de app (naam, iconen, kleuren, etc.)
2. **Service Worker** (`service-worker.js`) - Maakt offline functionaliteit en caching mogelijk
3. **PWA Meta Tags** - Voor betere ondersteuning op iOS en Android
4. **App Iconen** - 192x192px en 512x512px iconen voor verschillende schermresoluties
5. **Offline Support** - De app blijft (deels) werken zonder internetverbinding

## 📱 Installatie op Android (Chrome/Edge)

### Stappen:

1. **Open de website** in Chrome of Edge browser op je Android telefoon
2. **Wacht op de installatie prompt** - Je ziet automatisch een banner onderaan met "App installeren" of "Toevoegen aan beginscherm"
   - Als je deze niet ziet, ga dan naar stap 3
3. **Handmatig installeren via menu:**
   - Tik op het **menu-icoon** (drie puntjes) rechtsboven
   - Kies **"App installeren"** of **"Toevoegen aan startscherm"**
4. **Bevestig de installatie** door op "Installeren" te tikken
5. **Klaar!** De app verschijnt nu als icoon op je startscherm

### Verificatie:
- Het icoon van de app staat nu op je startscherm
- Open de app - deze opent nu in volledig scherm zonder adresbalk
- Je kunt de app wisselen tussen apps via de recente apps lijst

## 📱 Installatie op iOS (Safari)

### Stappen:

1. **Open de website** in Safari browser op je iPhone/iPad
2. **Tik op het deel-icoon** (vierkantje met pijltje omhoog) onderaan het scherm
3. **Scroll naar beneden** in het deel-menu
4. **Tik op "Zet op beginscherm"** (of "Add to Home Screen" in het Engels)
5. **Pas de naam aan** indien gewenst (standaard: "Ticker Dashboard")
6. **Tik op "Voeg toe"** rechtsboven
7. **Klaar!** De app verschijnt nu als icoon op je beginscherm

### iOS Opmerkingen:
- iOS Safari heeft beperktere PWA ondersteuning dan Android
- De app werkt wel volledig, maar sommige features zijn beperkt
- Je moet Safari gebruiken; Chrome op iOS ondersteunt geen PWA installatie

## 🔍 Hoe weet je of de installatie succesvol was?

### Android:
✅ App opent in volledig scherm (geen adresbalk)
✅ App verschijnt in je app drawer
✅ App is vindbaar via Android app-overzicht
✅ Je ziet een splash screen bij het openen

### iOS:
✅ App opent in volledig scherm (geen Safari UI)
✅ Icoon staat op het beginscherm
✅ App heeft eigen naam en icoon

## 🚀 PWA Features die nu werken:

### ✨ Installeerbaar
- De app kan op je startscherm geplaatst worden
- Werkt als een "native" app

### 📴 Offline Support (Gedeeltelijk)
- Statische bestanden (CSS, JS, iconen) worden gecached
- Pagina's die je al bezocht hebt blijven werken
- Nieuwe data vereist nog steeds een internetverbinding (door de authenticatie en API calls)

### 🎨 Native Look & Feel
- Volledig scherm ervaring
- Eigen theme color in de statusbalk
- Splash screen bij het laden

### 🔄 Automatische Updates
- Service worker update automatisch naar nieuwe versies
- Cache wordt automatisch ververst

## 🛠️ Troubleshooting

### "Installeer app" optie verschijnt niet (Android)
**Oplossingen:**
- Zorg dat je Chrome of Edge gebruikt (niet Firefox of andere browsers)
- Controleer of je via HTTPS verbindt (niet HTTP)
- Probeer de pagina te verversen
- Gebruik het menu → "App installeren" handmatig

### App laadt niet offline
**Oorzaken:**
- De app vereist authenticatie via Azure AD, wat een actieve internetverbinding nodig heeft
- Alleen al eerder bezochte pagina's zijn gedeeltelijk offline beschikbaar
- API data kan niet offline opgehaald worden

### Service Worker errors in console
**Oplossingen:**
- Controleer of de website via HTTPS draait (vereist voor Service Workers)
- Clear de browser cache en probeer opnieuw
- Check de browser console voor specifieke foutmeldingen

### iOS: "Zet op beginscherm" optie niet zichtbaar
**Oplossingen:**
- Gebruik Safari browser (Chrome werkt niet op iOS voor PWA's)
- Tik op het **deel-icoon** onderaan (niet het menu bovenaan)
- Scroll in het deel-menu naar beneden

## 🔐 Belangrijke Opmerkingen

1. **Authenticatie vereist internet** - Door Azure AD authenticatie heb je altijd een internetverbinding nodig om in te loggen
2. **HTTPS vereist** - PWA's werken alleen over HTTPS (of localhost voor development)
3. **Browser Support**:
   - ✅ Android: Chrome, Edge, Samsung Internet
   - ✅ iOS: Safari (alleen)
   - ❌ iOS: Chrome/Firefox (geen PWA support)

## 📞 Hulp nodig?

Als je problemen ondervindt bij de installatie:
1. Controleer of je de laatste versie van je browser gebruikt
2. Zorg dat de app via HTTPS draait
3. Check de browser console voor foutmeldingen (F12 op desktop)
4. Probeer de browser cache te legen

## 🎉 Geniet van je PWA!

Je dashboard werkt nu als een echte app op je telefoon! De service worker zorgt voor snellere laadtijden en een betere ervaring, zelfs bij een trage verbinding.

---

**Technische Details voor ontwikkelaars:**
- Manifest: `/manifest.json`
- Service Worker: `/service-worker.js`
- Icons: `/icon-192x192.png` en `/icon-512x512.png`
- Cache Strategy: Network First met fallback naar cache
