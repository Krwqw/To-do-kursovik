const AppConfig = {
  apiBaseUrl: (() => {
    const { protocol, hostname, port } = window.location;
    const isStaticPreview = protocol === 'file:' || port === '5500' || port === '3000' || port === '5173';
    return isStaticPreview ? 'https://localhost:7029/api' : `${window.location.origin}/api`;
  })(),
  author: 'Ковалева Ульяна',
  group: '3-1 ИС'
};

const AppState = {
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
