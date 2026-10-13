#nullable enable
using System;

namespace NovaBrowser;

public static class PrivateHomeHtml
{
    public static string GetContent()
    {
        return RawHtml;
    }

    public static string GetThemedContent(string accentHex = "#BEF47B", string bgHex = "#101216")
    {
        return RawHtml;
    }

    private const string RawHtml = """
<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Приватный режим</title>
<style>
:root{
  --bg-base:#101216;
  --bg-gradient:radial-gradient(circle at 50% 15%, #1a1e28 0%, #101216 75%);
  --card-bg:rgba(24, 28, 36, 0.75);
  --card-border:rgba(255, 255, 255, 0.08);
  --card-hover:rgba(32, 38, 50, 0.85);
  --text-main:#f3f4f6;
  --text-muted:#9ca3af;
  --text-dim:#6b7280;
  --accent:#bef47b;
  --accent-glow:rgba(190, 244, 123, 0.22);
  --badge-bg:rgba(255, 255, 255, 0.06);
  --badge-border:rgba(255, 255, 255, 0.1);
  --success:#bef47b;
}
*{box-sizing:border-box;margin:0;padding:0;user-select:none;-webkit-user-select:none}
body{
  background:var(--bg-base);
  background-image:var(--bg-gradient);
  color:var(--text-main);
  font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Helvetica,Arial,sans-serif;
  min-height:100vh;
  display:flex;
  flex-direction:column;
  align-items:center;
  justify-content:center;
  padding:32px 20px 48px;
  overflow-x:hidden;
}
.priv-container{
  width:100%;
  max-width:880px;
  display:flex;
  flex-direction:column;
  align-items:center;
  gap:28px;
}
.priv-badge-icon{
  width:88px;
  height:88px;
  border-radius:50%;
  background:rgba(255, 255, 255, 0.04);
  border:1px solid rgba(255, 255, 255, 0.12);
  display:flex;
  align-items:center;
  justify-content:center;
  box-shadow:0 12px 36px rgba(0,0,0,0.5), 0 0 24px rgba(255,255,255,0.03);
  transition:all 0.3s ease;
}
.priv-badge-icon:hover{
  transform:scale(1.05);
  border-color:rgba(255, 255, 255, 0.25);
  box-shadow:0 16px 48px rgba(0,0,0,0.6), 0 0 32px rgba(255,255,255,0.08);
}
.priv-badge-icon svg{
  width:46px;
  height:46px;
  fill:none;
  stroke:var(--text-main);
  stroke-width:1.8;
  stroke-linecap:round;
  stroke-linejoin:round;
}
.priv-header{
  text-align:center;
  display:flex;
  flex-direction:column;
  gap:10px;
}
.priv-title{
  font-size:32px;
  font-weight:700;
  letter-spacing:-0.02em;
  color:#ffffff;
}
.priv-subtitle{
  font-size:15px;
  color:var(--text-muted);
  max-width:540px;
  line-height:1.55;
}
.priv-search-wrap{
  width:100%;
  max-width:680px;
  display:flex;
  flex-direction:column;
  gap:12px;
}
.priv-search-box{
  position:relative;
  display:flex;
  align-items:center;
  background:rgba(26, 30, 40, 0.9);
  backdrop-filter:blur(16px);
  -webkit-backdrop-filter:blur(16px);
  border:1px solid rgba(255, 255, 255, 0.12);
  border-radius:16px;
  padding:4px 8px 4px 18px;
  transition:all 0.2s ease;
  box-shadow:0 8px 28px rgba(0,0,0,0.4);
}
.priv-search-box:focus-within{
  border-color:rgba(255, 255, 255, 0.35);
  box-shadow:0 12px 36px rgba(0,0,0,0.5), 0 0 0 3px rgba(255,255,255,0.06);
  background:rgba(30, 36, 48, 0.95);
}
.priv-search-icon{
  width:20px;
  height:20px;
  stroke:var(--text-muted);
  fill:none;
  stroke-width:2;
  stroke-linecap:round;
  stroke-linejoin:round;
  flex-shrink:0;
  margin-right:12px;
}
.priv-search-input{
  flex:1;
  background:transparent;
  border:none;
  outline:none;
  font-size:16px;
  color:#ffffff;
  padding:12px 0;
  font-family:inherit;
}
.priv-search-input::placeholder{
  color:var(--text-dim);
}
.priv-search-submit{
  background:rgba(255, 255, 255, 0.08);
  border:1px solid rgba(255, 255, 255, 0.12);
  border-radius:10px;
  color:var(--text-main);
  padding:8px 16px;
  font-size:13px;
  font-weight:600;
  cursor:pointer;
  transition:all 0.18s;
  display:flex;
  align-items:center;
  gap:6px;
}
.priv-search-submit:hover{
  background:rgba(255, 255, 255, 0.16);
  border-color:rgba(255, 255, 255, 0.25);
  color:#ffffff;
}
.priv-engines{
  display:flex;
  align-items:center;
  justify-content:center;
  gap:8px;
  flex-wrap:wrap;
}
.engine-chip{
  background:rgba(255, 255, 255, 0.04);
  border:1px solid rgba(255, 255, 255, 0.08);
  border-radius:999px;
  color:var(--text-muted);
  padding:5px 14px;
  font-size:12px;
  cursor:pointer;
  transition:all 0.15s;
}
.engine-chip:hover{
  background:rgba(255, 255, 255, 0.08);
  color:var(--text-main);
  border-color:rgba(255, 255, 255, 0.15);
}
.engine-chip.active{
  background:rgba(255, 255, 255, 0.14);
  color:#ffffff;
  border-color:rgba(255, 255, 255, 0.28);
  font-weight:600;
}
.priv-cards-row{
  display:grid;
  grid-template-columns:repeat(auto-fit, minmax(320px, 1fr));
  gap:20px;
  width:100%;
}
.priv-card{
  background:var(--card-bg);
  backdrop-filter:blur(14px);
  -webkit-backdrop-filter:blur(14px);
  border:1px solid var(--card-border);
  border-radius:18px;
  padding:24px 22px;
  display:flex;
  flex-direction:column;
  gap:16px;
  box-shadow:0 8px 24px rgba(0,0,0,0.35);
  transition:all 0.2s;
}
.priv-card:hover{
  border-color:rgba(255, 255, 255, 0.15);
  background:var(--card-hover);
  transform:translateY(-2px);
}
.card-head{
  display:flex;
  align-items:center;
  gap:12px;
}
.card-icon-circle{
  width:36px;
  height:36px;
  border-radius:10px;
  background:rgba(255, 255, 255, 0.06);
  border:1px solid rgba(255, 255, 255, 0.1);
  display:flex;
  align-items:center;
  justify-content:center;
  color:var(--text-main);
}
.card-icon-circle svg{
  width:20px;
  height:20px;
  fill:none;
  stroke:currentColor;
  stroke-width:2;
  stroke-linecap:round;
  stroke-linejoin:round;
}
.card-title{
  font-size:16px;
  font-weight:700;
  color:#ffffff;
}
.priv-list{
  list-style:none;
  display:flex;
  flex-direction:column;
  gap:12px;
}
.priv-list li{
  display:flex;
  align-items:flex-start;
  gap:10px;
  font-size:13.5px;
  line-height:1.5;
  color:var(--text-muted);
}
.priv-list strong{
  color:var(--text-main);
  font-weight:600;
}
.list-bullet{
  width:6px;
  height:6px;
  border-radius:50%;
  background:var(--text-dim);
  margin-top:7px;
  flex-shrink:0;
}
.priv-card.safe .list-bullet{
  background:#10b981;
  box-shadow:0 0 8px rgba(16, 185, 129, 0.5);
}
.priv-card.notice .list-bullet{
  background:#f59e0b;
  box-shadow:0 0 8px rgba(245, 158, 11, 0.4);
}
.priv-status-bar{
  display:flex;
  align-items:center;
  justify-content:center;
  gap:16px;
  flex-wrap:wrap;
  width:100%;
  padding-top:4px;
}
.status-pill{
  display:flex;
  align-items:center;
  gap:8px;
  background:rgba(255, 255, 255, 0.03);
  border:1px solid rgba(255, 255, 255, 0.06);
  padding:6px 14px;
  border-radius:999px;
  font-size:12px;
  color:var(--text-dim);
}
.status-dot{
  width:7px;
  height:7px;
  border-radius:50%;
  background:#10b981;
  box-shadow:0 0 6px rgba(16, 185, 129, 0.6);
}
.status-label{
  color:var(--text-muted);
}
.status-state{
  color:var(--text-main);
  font-weight:600;
}
.priv-shortcuts{
  display:flex;
  gap:12px;
  flex-wrap:wrap;
  justify-content:center;
  margin-top:6px;
}
.shortcut-btn{
  background:rgba(255, 255, 255, 0.04);
  border:1px solid rgba(255, 255, 255, 0.08);
  border-radius:12px;
  padding:8px 16px;
  display:flex;
  align-items:center;
  gap:8px;
  color:var(--text-muted);
  font-size:12.5px;
  cursor:pointer;
  transition:all 0.16s;
  text-decoration:none;
}
.shortcut-btn:hover{
  background:rgba(255, 255, 255, 0.09);
  border-color:rgba(255, 255, 255, 0.18);
  color:#ffffff;
  transform:translateY(-1px);
}
.shortcut-fav{
  width:16px;
  height:16px;
  border-radius:3px;
  object-fit:contain;
}
</style>
</head>
<body>
<div class="priv-container">
  <div class="priv-badge-icon">
    <svg viewBox="0 0 24 24">
      <path d="M12 2L4 6v6c0 5.5 3.8 10.7 8 12 4.2-1.3 8-6.5 8-12V6l-8-4z"/>
      <path d="M9 12l2 2 4-4"/>
    </svg>
  </div>

  <div class="priv-header">
    <h1 class="priv-title">Приватный режим Nova</h1>
    <p class="priv-subtitle">Вы перешли в изолированный приватный сеанс. Данные этой вкладки не будут сохранены на вашем компьютере после её закрытия.</p>
  </div>

  <div class="priv-search-wrap">
    <div class="priv-search-box">
      <svg class="priv-search-icon" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
      <input type="text" id="privSearchInput" class="priv-search-input" placeholder="Приватный поиск или веб-адрес..." autocomplete="off" autofocus>
      <button class="priv-search-submit" id="btnPrivSearch" type="button">
        <span>Перейти</span>
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
      </button>
    </div>
    <div class="priv-engines">
      <button class="engine-chip active" data-engine="nova" type="button">Nova Mix</button>
      <button class="engine-chip" data-engine="google" type="button">Google</button>
      <button class="engine-chip" data-engine="yandex" type="button">Яндекс</button>
      <button class="engine-chip" data-engine="duckduckgo" type="button">DuckDuckGo</button>
    </div>
  </div>

  <div class="priv-cards-row">
    <div class="priv-card safe">
      <div class="card-head">
        <div class="card-icon-circle">
          <svg viewBox="0 0 24 24"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>
        </div>
        <div class="card-title">Что защищено в сессии</div>
      </div>
      <ul class="priv-list">
        <li>
          <span class="list-bullet"></span>
          <div><strong>История просмотров:</strong> посещенные страницы не попадают в историю браузера.</div>
        </li>
        <li>
          <span class="list-bullet"></span>
          <div><strong>Файлы Cookie:</strong> сессионные куки удаляются сразу при закрытии вкладки.</div>
        </li>
        <li>
          <span class="list-bullet"></span>
          <div><strong>Данные форм:</strong> логины, пароли и поисковые запросы не кэшируются.</div>
        </li>
        <li>
          <span class="list-bullet"></span>
          <div><strong>Изолированная память:</strong> нулевой след в системном профиле.</div>
        </li>
      </ul>
    </div>

    <div class="priv-card notice">
      <div class="card-head">
        <div class="card-icon-circle">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
        </div>
        <div class="card-title">Что следует учитывать</div>
      </div>
      <ul class="priv-list">
        <li>
          <span class="list-bullet"></span>
          <div><strong>Сайты и сервисы:</strong> видят ваш IP-адрес и авторизации, выполненные в этой вкладке.</div>
        </li>
        <li>
          <span class="list-bullet"></span>
          <div><strong>Сетевой провайдер:</strong> администратор Wi-Fi или провайдер фиксируют факт подключений.</div>
        </li>
        <li>
          <span class="list-bullet"></span>
          <div><strong>Скачанные файлы:</strong> загруженные файлы остаются на жестком диске.</div>
        </li>
      </ul>
    </div>
  </div>

  <div class="priv-shortcuts">
    <a class="shortcut-btn" href="#" data-nav="https://duckduckgo.com">
      <img class="shortcut-fav" src="https://icons.duckduckgo.com/ip3/duckduckgo.com.ico" alt="">
      <span>DuckDuckGo</span>
    </a>
    <a class="shortcut-btn" href="#" data-nav="https://github.com">
      <img class="shortcut-fav" src="https://icons.duckduckgo.com/ip3/github.com.ico" alt="">
      <span>GitHub</span>
    </a>
    <a class="shortcut-btn" href="#" data-nav="https://ru.wikipedia.org">
      <img class="shortcut-fav" src="https://icons.duckduckgo.com/ip3/ru.wikipedia.org.ico" alt="">
      <span>Википедия</span>
    </a>
    <a class="shortcut-btn" href="#" data-nav="https://habr.com">
      <img class="shortcut-fav" src="https://icons.duckduckgo.com/ip3/habr.com.ico" alt="">
      <span>Хабр</span>
    </a>
    <a class="shortcut-btn" href="#" data-nav="https://reddit.com">
      <img class="shortcut-fav" src="https://icons.duckduckgo.com/ip3/reddit.com.ico" alt="">
      <span>Reddit</span>
    </a>
  </div>

  <div class="priv-status-bar">
    <div class="status-pill">
      <span class="status-dot"></span>
      <span class="status-label">Защита от трекеров:</span>
      <span class="status-state">АКТИВНА</span>
    </div>
    <div class="status-pill">
      <span class="status-dot"></span>
      <span class="status-label">Изоляция Cookie:</span>
      <span class="status-state">ВКЛЮЧЕНА</span>
    </div>
    <div class="status-pill">
      <span class="status-dot"></span>
      <span class="status-label">Блокировка рекламы:</span>
      <span class="status-state">АКТИВНА</span>
    </div>
    <div class="status-pill">
      <span class="status-dot"></span>
      <span class="status-label">Anti-Fingerprint:</span>
      <span class="status-state">АКТИВЕН</span>
    </div>
  </div>
</div>

<script>
(function(){
  var currentEngine = 'nova';
  var input = document.getElementById('privSearchInput');
  var btn = document.getElementById('btnPrivSearch');
  var engineChips = document.querySelectorAll('.engine-chip');

  function navigateToUrl(url){
    if(!url) return;
    url = url.trim();
    if(!/^https?:\/\//i.test(url) && !/^about:/i.test(url) && !/^nova:\/\//i.test(url)){
      url = 'https://' + url;
    }
    if(window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function'){
      try {
        window.chrome.webview.postMessage({ type: 'navigate', url: url });
        return;
      } catch(e){}
    }
    window.location.href = url;
  }

  engineChips.forEach(function(chip){
    chip.addEventListener('click', function(){
      engineChips.forEach(function(c){ c.classList.remove('active'); });
      chip.classList.add('active');
      currentEngine = chip.getAttribute('data-engine') || 'nova';
      if(input) input.focus();
    });
  });

  function performSearch(){
    if(!input) return;
    var raw = input.value.trim();
    if(!raw) return;

    if(/^https?:\/\//i.test(raw)){
      navigateToUrl(raw);
      return;
    }
    if(/^[a-z0-9-]+(\.[a-z0-9-]+)+/i.test(raw) && !raw.includes(' ')){
      navigateToUrl('https://' + raw);
      return;
    }

    var enc = encodeURIComponent(raw);
    var target = 'https://www.google.com/search?q=' + enc;
    if(currentEngine === 'yandex'){
      target = 'https://yandex.ru/search/?text=' + enc;
    } else if(currentEngine === 'duckduckgo'){
      target = 'https://duckduckgo.com/?q=' + enc;
    } else if(currentEngine === 'google'){
      target = 'https://www.google.com/search?q=' + enc;
    } else if(currentEngine === 'nova'){
      target = 'https://nova.example/index.html#/search?q=' + enc;
    }
    navigateToUrl(target);
  }

  if(btn){
    btn.addEventListener('click', performSearch);
  }
  if(input){
    input.addEventListener('keydown', function(e){
      if(e.key === 'Enter') performSearch();
    });
  }

  document.querySelectorAll('[data-nav]').forEach(function(el){
    el.addEventListener('click', function(e){
      e.preventDefault();
      var u = el.getAttribute('data-nav');
      if(u) navigateToUrl(u);
    });
  });

  window.triggerSearch = function(q){
    if(input) input.value = q;
    performSearch();
  };

  if(window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function'){
    try { window.chrome.webview.postMessage({ type: 'ready' }); } catch(e){}
  }
})();
</script>
</body>
</html>

""";
}
