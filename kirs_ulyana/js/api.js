const Api = {
  async request(path, options = {}) {
    const headers = {
      'Content-Type': 'application/json',
      ...(options.headers || {})
    };

    if (AppState.token) {
      headers.Authorization = `Bearer ${AppState.token}`;
    }

    const response = await fetch(`${AppConfig.apiBaseUrl}${path}`, {
      ...options,
      headers
    });

    if (response.status === 401) {
      Auth.logout(false);
      throw new Error('Сессия истекла. Войдите снова.');
    }

    const contentType = response.headers.get('content-type') || '';
    const data = contentType.includes('application/json') ? await response.json() : null;

    if (!response.ok) {
      throw new Error(data?.message || data?.title || 'Ошибка запроса к серверу.');
    }

    return data;
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
