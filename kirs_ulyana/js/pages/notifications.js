const Notifications = {
  items: [],

  async load() {
    const list = document.getElementById('notif-list');
    list.innerHTML = '<p class="notif-empty">Загрузка...</p>';

    try {
      this.items = await Api.get('/notifications');
      this.render();
      this.renderBadge();
    } catch (error) {
      list.innerHTML = `<p class="notif-empty">${escapeHtml(error.message)}</p>`;
    }
  },

  async updateBadge() {
    try {
      this.items = await Api.get('/notifications');
      this.renderBadge();
    } catch {
      this.items = [];
      this.renderBadge();
    }
  },

  render() {
    const list = document.getElementById('notif-list');
    if (!this.items.length) {
      list.innerHTML = '<p class="notif-empty">Уведомлений пока нет</p>';
      return;
    }

    list.innerHTML = this.items.map((notification) => `
      <article class="notif-item ${notification.isRead ? '' : 'unread'}" onclick="Notifications.openTask(${notification.id}, ${notification.taskId})">
        <span class="notif-dot ${notification.isRead ? 'read' : ''}"></span>
        <div class="notif-body">
          <p class="notif-text">${escapeHtml(notification.text)}</p>
          <span class="notif-date">${formatDate(notification.createdAt)}</span>
        </div>
      </article>
    `).join('');
  },

  renderBadge() {
    const badge = document.getElementById('notif-badge');
    const unread = this.items.filter((item) => !item.isRead).length;
    badge.textContent = unread;
    badge.classList.toggle('hidden', unread === 0);
  },

  async openTask(notificationId, taskId) {
    try {
      await Api.put(`/notifications/${notificationId}/read`);
      await this.updateBadge();
      Router.go('task', { id: taskId });
    } catch (error) {
      Toast.error(error);
    }
  },

  async markAllRead() {
    try {
      await Api.put('/notifications/read-all');
      this.items = this.items.map((item) => ({ ...item, isRead: true }));
      this.render();
      this.renderBadge();
      Toast.show('Уведомления прочитаны.');
    } catch (error) {
      Toast.error(error);
    }
  }
};
