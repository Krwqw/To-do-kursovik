var DeadlineNotifier = {
  scanInProgress: false,
  scanTimer: null,

  init() {
    this.patchNotifications();
    this.patchBoardPages();
    this.scanTimer = window.setInterval(() => this.scanAllBoards(false), 60000);
    window.addEventListener('focus', () => this.scanAllBoards(false));
    window.setTimeout(() => this.scanAllBoards(false), 1000);
  },

  storageKey() {
    return `taskboard_deadline_notifications_${AppState.user?.id || 'guest'}`;
  },

  loadLocal() {
    try {
      return JSON.parse(localStorage.getItem(this.storageKey()) || '[]');
    } catch {
      return [];
    }
  },

  saveLocal(items) {
    localStorage.setItem(this.storageKey(), JSON.stringify(items));
  },

  combine(serverItems = []) {
    return [...this.loadLocal(), ...serverItems]
      .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
  },

  async scanAllBoards(showToast = true) {
    if (this.scanInProgress || !Auth.isLoggedIn()) {
      return;
    }

    this.scanInProgress = true;
    try {
      const boards = await Api.get('/boards');
      const taskGroups = await Promise.all(
        boards.map(async (board) => ({
          board,
          tasks: await Api.get(`/tasks?boardId=${board.id}`)
        }))
      );

      taskGroups.forEach(({ board, tasks }) => {
        tasks.forEach((task) => this.trackTask(task, board, showToast));
      });

      Notifications.renderBadge();
    } catch {
      // Backend может быть временно недоступен; существующие локальные уведомления остаются на месте.
    } finally {
      this.scanInProgress = false;
    }
  },

  scanCurrentBoard(showToast = true) {
    if (!AppState.currentBoard || !Array.isArray(Board.tasks)) {
      return;
    }

    Board.tasks.forEach((task) => this.trackTask(task, AppState.currentBoard, showToast));
    Notifications.items = this.combine(Notifications.items.filter((item) => !item.isLocal));
    Notifications.renderBadge();
  },

  trackTask(task, board, showToast = true) {
    if (!task?.dueDate || task.status === 'done') {
      return;
    }

    const dueTime = new Date(task.dueDate).getTime();
    if (Number.isNaN(dueTime) || dueTime > Date.now()) {
      return;
    }

    const id = -Math.abs(Number(task.id));
    const localItems = this.loadLocal();
    if (localItems.some((item) => item.id === id)) {
      return;
    }

    const text = `Дедлайн наступил: задача "${task.title}" просрочена.`;
    const notification = {
      id,
      taskId: task.id,
      boardId: board?.id || AppState.currentBoard?.id || null,
      text,
      isRead: false,
      isLocal: true,
      createdAt: new Date().toISOString(),
      dueDate: task.dueDate
    };

    this.saveLocal([notification, ...localItems]);
    if (showToast) {
      Toast.show(text, 'error');
    }
  },

  markLocalRead(notificationId) {
    const id = Number(notificationId);
    const updated = this.loadLocal().map((item) =>
      item.id === id ? { ...item, isRead: true } : item
    );
    this.saveLocal(updated);
  },

  markAllLocalRead() {
    this.saveLocal(this.loadLocal().map((item) => ({ ...item, isRead: true })));
  },

  patchNotifications() {
    const originalLoad = Notifications.load.bind(Notifications);
    const originalUpdateBadge = Notifications.updateBadge.bind(Notifications);
    const originalOpenTask = Notifications.openTask.bind(Notifications);
    const originalMarkAllRead = Notifications.markAllRead.bind(Notifications);

    Notifications.load = async () => {
      const list = document.getElementById('notif-list');
      list.innerHTML = '<p class="notif-empty">Загрузка...</p>';

      try {
        const serverItems = await Api.get('/notifications');
        Notifications.items = this.combine(serverItems);
      } catch {
        Notifications.items = this.combine([]);
      }

      Notifications.render();
      Notifications.renderBadge();
    };

    Notifications.updateBadge = async () => {
      try {
        const serverItems = await Api.get('/notifications');
        Notifications.items = this.combine(serverItems);
      } catch {
        Notifications.items = this.combine([]);
      }

      Notifications.renderBadge();
    };

    Notifications.openTask = async (notificationId, taskId) => {
      if (Number(notificationId) < 0) {
        this.markLocalRead(notificationId);
        Notifications.items = this.combine(Notifications.items.filter((item) => !item.isLocal));
        Notifications.renderBadge();
        Router.go('task', { id: taskId });
        return;
      }

      await originalOpenTask(notificationId, taskId);
    };

    Notifications.markAllRead = async () => {
      this.markAllLocalRead();

      try {
        await originalMarkAllRead();
      } catch {
        Toast.show('Локальные уведомления прочитаны.');
      }

      Notifications.items = this.combine(Notifications.items.filter((item) => !item.isLocal));
      Notifications.render();
      Notifications.renderBadge();
    };

    Notifications.loadServerOnly = originalLoad;
    Notifications.updateServerBadgeOnly = originalUpdateBadge;
  },

  patchBoardPages() {
    const originalRenderTasks = Board.renderTasks.bind(Board);
    Board.renderTasks = () => {
      originalRenderTasks();
      this.scanCurrentBoard(true);
    };

    const originalTaskRender = TaskDetail.render.bind(TaskDetail);
    TaskDetail.render = (task) => {
      originalTaskRender(task);
      this.trackTask(task, AppState.currentBoard, true);
      Notifications.updateBadge();
    };
  }
};

document.addEventListener('DOMContentLoaded', () => DeadlineNotifier.init());
