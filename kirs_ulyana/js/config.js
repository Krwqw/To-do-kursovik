var AppConfig = {
  apiBaseUrls: (() => {
    const { protocol, hostname, origin } = window.location;
    const urls = [];

    if (protocol === 'http:' || protocol === 'https:') {
      urls.push(`${origin}/api`);
    }

    if (hostname === 'localhost' || hostname === '127.0.0.1' || protocol === 'file:') {
      urls.push('http://localhost:5272/api');
      urls.push('https://localhost:7029/api');
      urls.push('http://127.0.0.1:5272/api');
    }

    return [...new Set(urls)];
  })(),
  apiBaseUrl: null,
  author: 'Ковалева Ульяна',
  group: '3-1 ИС'
};

var AppState = {
  user: null,
  token: localStorage.getItem('taskboard_token'),
  currentBoard: null,
  currentTask: null
};

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}

function formatDate(value) {
  if (!value) {
    return '—';
  }

  return new Date(value).toLocaleDateString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
}

function statusText(status) {
  return {
    todo: 'To Do',
    inprogress: 'In Progress',
    done: 'Done'
  }[status] || 'To Do';
}
