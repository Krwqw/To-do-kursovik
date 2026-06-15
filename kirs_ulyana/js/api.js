var Api = {
  async request(path, options = {}) {
    const headers = {
      'Content-Type': 'application/json',
      ...(options.headers || {})
    };

    if (AppState.token) {
      headers.Authorization = `Bearer ${AppState.token}`;
    }

    const preferredUrl = AppConfig.apiBaseUrl ? [AppConfig.apiBaseUrl] : [];
    const candidateUrls = [...new Set([...preferredUrl, ...AppConfig.apiBaseUrls])];
    let lastNetworkError = null;

    for (const baseUrl of candidateUrls) {
      try {
        const response = await fetch(`${baseUrl}${path}`, {
          ...options,
          headers
        });

        AppConfig.apiBaseUrl = baseUrl;

        if (response.status === 401) {
          Auth.logout(false);
          throw new Error('Сессия истекла. Войдите снова.');
        }

        const contentType = response.headers.get('content-type') || '';
        const data = contentType.includes('application/json') ? await response.json() : null;
        const text = data ? '' : await response.text();

        if (!response.ok) {
          throw new Error(data?.message || data?.title || getServerErrorText(text) || 'Ошибка запроса к серверу.');
        }

        return data;
      } catch (error) {
        if (error instanceof TypeError) {
          lastNetworkError = error;
          continue;
        }

        throw error;
      }
    }

    throw new Error(lastNetworkError
      ? 'Не удалось подключиться к backend. Запустите проект Kursovichok2 и обновите страницу.'
      : 'Не указан адрес backend.');
  },

  get(path) {
    return this.request(path);
  },

  post(path, body) {
    return this.request(path, {
      method: 'POST',
      body: JSON.stringify(body)
    });
  },

  put(path, body = {}) {
    return this.request(path, {
      method: 'PUT',
      body: JSON.stringify(body)
    });
  },

  delete(path) {
    return this.request(path, { method: 'DELETE' });
  }
};

function getServerErrorText(text) {
  if (!text) {
    return '';
  }

  const plainText = text.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
  const firstSentence = plainText.split(' at ')[0];
  return firstSentence ? `Ошибка backend: ${firstSentence.slice(0, 220)}` : '';
}
