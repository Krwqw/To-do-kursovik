var Board = {
  tasks: [],

  async load(boardId) {
    if (!boardId) {
      Router.go('boards');
      return;
    }

    try {
      const [board, tasks] = await Promise.all([
        Api.get(`/boards/${boardId}`),
        Api.get(`/boards/${boardId}/tasks`)
      ]);
      AppState.currentBoard = board;
      this.tasks = tasks;
      this.renderHeader(board);
      this.renderTasks();
    } catch (error) {
      Toast.error(error);
      Router.go('boards');
    }
  },

  renderHeader(board) {
    document.getElementById('board-title').textContent = board.title;
    document.getElementById('board-desc').textContent = board.description || 'Без описания';
  },

  renderTasks() {
    const statuses = ['todo', 'inprogress', 'done'];
    statuses.forEach((status) => {
      const cards = this.tasks.filter((task) => task.status === status);
      document.getElementById(`count-${status}`).textContent = cards.length;
      document.getElementById(`cards-${status}`).innerHTML = cards.length
        ? cards.map((task) => this.renderTaskCard(task)).join('')
        : '<p class="comment-empty">Нет задач</p>';
    });
  },

  renderTaskCard(task) {
    const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'done';
    return `
      <article class="task-card" onclick="Router.go('task', { id: ${task.id} })">
        <h3 class="task-card-title">${escapeHtml(task.title)}</h3>
        <div class="task-card-due ${isOverdue ? 'overdue' : ''}">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2"/><path d="M16 2v4M8 2v4M3 10h18"/></svg>
          ${formatDate(task.dueDate)}
        </div>
        <div class="task-card-assignee">Ответственный: ${escapeHtml(task.assigneeName || 'не назначен')}</div>
      </article>
    `;
  },

  openEditModal() {
    Boards.openEditModal(AppState.currentBoard?.id);
  },

  openTaskModal() {
    document.getElementById('task-name-input').value = '';
    document.getElementById('task-desc-input').value = '';
    document.getElementById('task-status-input').value = 'todo';
    document.getElementById('task-due-input').value = '';
    Modal.open('modal-task');
  },

  async submitTask() {
    const title = document.getElementById('task-name-input').value.trim();
    const description = document.getElementById('task-desc-input').value.trim();
    const status = document.getElementById('task-status-input').value;
    const dueDate = document.getElementById('task-due-input').value || null;

    if (!title) {
      Toast.show('Введите название задачи.', 'error');
      return;
    }

    try {
      const task = await Api.post('/tasks', {
        title,
        description,
        status,
        dueDate,
        boardId: AppState.currentBoard.id
      });
      this.tasks.push(task);
      Modal.close('modal-task');
      Toast.show('Задача создана.');
      this.renderTasks();
    } catch (error) {
      Toast.error(error);
    }
  },

  goBack() {
    Router.go('board', { id: AppState.currentBoard?.id });
  }
};
