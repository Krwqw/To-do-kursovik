var TaskDetail = {
  async load(taskId) {
    if (!taskId) {
      Router.go('boards');
      return;
    }

    try {
      const task = await Api.get(`/tasks/${taskId}`);
      AppState.currentTask = task;
      this.render(task);
    } catch (error) {
      Toast.error(error);
      Board.goBack();
    }
  },

  render(task) {
    document.getElementById('task-title').textContent = task.title;
    document.getElementById('task-description').textContent = task.description || 'Описание не заполнено.';
    document.getElementById('task-due').textContent = formatDate(task.dueDate);
    document.getElementById('task-created').textContent = formatDate(task.createdAt);
    document.getElementById('task-assignee').textContent = AppState.user?.userName || '—';
    document.getElementById('task-status-select').value = task.status;

    const badge = document.getElementById('task-status-badge');
    badge.className = `status-badge status-${task.status}`;
    badge.textContent = statusText(task.status);

    const comments = document.getElementById('task-comments');
    comments.innerHTML = task.comments?.length
      ? task.comments.map((comment) => `
          <article class="comment-item">
            <div class="comment-header">
              <span class="comment-author">${escapeHtml(comment.authorName || 'Пользователь')}</span>
              <span class="comment-date">${formatDate(comment.createdAt)}</span>
            </div>
            <p class="comment-text">${escapeHtml(comment.text)}</p>
          </article>
        `).join('')
      : '<p class="comment-empty">Комментариев пока нет</p>';
  },

  openEditModal() {
    const task = AppState.currentTask;
    if (!task) {
      return;
    }

    document.getElementById('edit-task-name').value = task.title;
    document.getElementById('edit-task-desc').value = task.description || '';
    document.getElementById('edit-task-status').value = task.status;
    document.getElementById('edit-task-due').value = task.dueDate ? task.dueDate.slice(0, 10) : '';
    Modal.open('modal-edit-task');
  },

  async submitEdit() {
    const task = AppState.currentTask;
    const title = document.getElementById('edit-task-name').value.trim();
    const description = document.getElementById('edit-task-desc').value.trim();
    const status = document.getElementById('edit-task-status').value;
    const dueDate = document.getElementById('edit-task-due').value || null;

    if (!title) {
      Toast.show('Введите название задачи.', 'error');
      return;
    }

    try {
      const updated = await Api.put(`/tasks/${task.id}`, { title, description, status, dueDate });
      AppState.currentTask = updated;
      Modal.close('modal-edit-task');
      Toast.show('Задача обновлена.');
      this.render(updated);
    } catch (error) {
      Toast.error(error);
    }
  },

  async changeStatus(status) {
    const task = AppState.currentTask;
    if (!task || task.status === status) {
      return;
    }

    try {
      const updated = await Api.put(`/tasks/${task.id}`, {
        title: task.title,
        description: task.description,
        status,
        dueDate: task.dueDate
      });
      AppState.currentTask = updated;
      Toast.show('Статус изменён.');
      this.render(updated);
    } catch (error) {
      Toast.error(error);
    }
  },

  async addComment() {
    const textarea = document.getElementById('comment-text');
    const text = textarea.value.trim();
    const task = AppState.currentTask;

    if (!text) {
      Toast.show('Введите текст комментария.', 'error');
      return;
    }

    try {
      const comment = await Api.post(`/comments`, { text: text, taskId: task.id });
      task.comments = [...(task.comments || []), comment];
      textarea.value = '';
      Toast.show('Комментарий добавлен.');
      this.render(task);
    } catch (error) {
      Toast.error(error);
    }
  },

  async deleteTask() {
    const task = AppState.currentTask;
    Modal.confirm('Удалить задачу?', 'Задача и её комментарии будут удалены.', async () => {
      try {
        await Api.delete(`/tasks/${task.id}`);
        Toast.show('Задача удалена.');
        Router.go('board', { id: AppState.currentBoard?.id });
      } catch (error) {
        Toast.error(error);
      }
    });
  }
};
