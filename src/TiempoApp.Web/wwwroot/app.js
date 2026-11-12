// TiempoApp front-end: calls /api/weather and renders the card. No build step, no dependencies.
'use strict';

const form = document.querySelector('#search-form');
const input = document.querySelector('#city');
const button = document.querySelector('#search-button');
const result = document.querySelector('#result');
const chips = document.querySelectorAll('.chip');
const $ = (id) => document.getElementById(id);

const UNITS = {
  metric: { temp: '°C', wind: (ms) => `${Math.round(ms * 3.6)} km/h` },
  imperial: { temp: '°F', wind: (mph) => `${Math.round(mph)} mph` },
  standard: { temp: 'K', wind: (ms) => `${ms.toFixed(1)} m/s` },
};

const regionNames = new Intl.DisplayNames(['es'], { type: 'region' });
let controller;

function setState(state) {
  for (const el of result.querySelectorAll('[data-state]')) {
    el.hidden = el.dataset.state !== state;
  }
  const loading = state === 'loading';
  result.setAttribute('aria-busy', String(loading));
  form.classList.toggle('is-loading', loading);
  button.disabled = loading;
  for (const chip of chips) chip.disabled = loading;
}

function countryName(code) {
  try { return code ? regionNames.of(code) : ''; } catch { return code; }
}

// observedAt carries the city's UTC offset ("2026-11-12T11:10:08-05:00"): show the city's local time as-is.
function localTime(observedAt) {
  const match = /T(\d{2}:\d{2})/.exec(observedAt ?? '');
  return match ? `Actualizado ${match[1]} (hora local)` : '';
}

function render(w) {
  const units = UNITS[w.units] ?? UNITS.metric;
  $('w-city').textContent = w.city;
  $('w-country').textContent = countryName(w.country);
  $('w-time').textContent = localTime(w.observedAt);
  $('w-icon').src = w.iconUrl;
  $('w-icon').alt = w.description;
  $('w-temp').textContent = Math.round(w.temperature);
  $('w-unit').textContent = units.temp;
  $('w-desc').textContent = w.description;
  $('w-feels').textContent = `${Math.round(w.feelsLike)} ${units.temp}`;
  $('w-humidity').textContent = `${w.humidity} %`;
  $('w-wind').textContent = units.wind(w.windSpeed);
}

function showError(title, detail) {
  $('error-title').textContent = title;
  $('error-detail').textContent = detail;
  setState('error');
}

// ProblemDetails / ValidationProblemDetails -> user message.
function problemText(problem, status) {
  return problem?.errors?.city?.[0] ?? problem?.detail ?? `El servidor respondió con el código ${status}.`;
}

async function search(city) {
  controller?.abort();
  controller = new AbortController();
  setState('loading');

  try {
    const response = await fetch(`/api/weather?city=${encodeURIComponent(city)}`, {
      headers: { Accept: 'application/json' },
      signal: controller.signal,
    });
    const body = await response.json().catch(() => null);

    if (!response.ok) {
      showError(body?.title ?? 'No se pudo obtener el clima', problemText(body, response.status));
      return;
    }

    render(body);
    setState('success');
    history.replaceState(null, '', `?city=${encodeURIComponent(city)}`);
  } catch (error) {
    if (error.name === 'AbortError') return;
    showError('Sin conexión', 'No se pudo conectar con el servidor. Comprueba tu conexión e inténtalo de nuevo.');
  }
}

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const city = input.value.trim();
  if (!city) {
    showError('Falta la ciudad', 'Escribe el nombre de una ciudad.');
    input.focus();
    return;
  }
  search(city);
});

// Example city chips: fill the box and search.
for (const chip of chips) {
  chip.addEventListener('click', () => {
    input.value = chip.dataset.city;
    search(chip.dataset.city);
  });
}

// Deep link: /?city=Quito loads directly (handy for the demo).
const initialCity = new URLSearchParams(location.search).get('city')?.trim();
if (initialCity) {
  input.value = initialCity;
  search(initialCity);
} else {
  input.focus();
}
