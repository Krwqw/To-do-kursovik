const Router = {
  currentPage: null,

  go(page, params = {}) {
    const authView = document.getElementById('auth-view');
    const appView = document.getElementById('app-view');

    document.querySelectorAll('.auth-card, .page').forEach((element) => element.classList.add('hidden'));
    document.querySelectorAll('.nav-item').forEach((item) => item.classList.remove('active'));

    if (page === 'login' || page === 'register') {
      authView.classList.remove('hidden');
      appView.classList.add('hidden');
      document.getElementById(`page-${page}`).classList.remove('hidden');
      this.currentPage = page;
      return;
    }

    if (!Auth.isLoggedIn()) {
      this.go('login');
      return;
    }

    authView.classList.add('hidden');
    appView.classList.remove('hidden');
    document.getElementById(`page-${page}`).classList.remove('hidden');
    document.querySelectorAll(`[data-page="${page}"]`).forEach((item) => item.classList.add('active'));
    this.currentPage = page;

    if (page === 'boards') {
      Boards.load();
      Notifications.updateBadge();
    }

    if (page === 'board') {
      Board.load(params.id ?? AppState.currentBoard?.id);
    }

    if (page === 'task') {
      TaskDetail.load(params.id ?? AppState.currentTask?.id);
    }

    if (page === 'notifications') {
      Notifications.load();
    }

    if (page === 'profile') {
      Auth.refreshProfile();
    }
  }
};
