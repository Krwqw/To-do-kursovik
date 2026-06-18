var Auth = {
  init() {
    const savedUser = localStorage.getItem('taskboard_user');
    AppState.user = savedUser ? JSON.parse(savedUser) : null;
  },

  isLoggedIn() {
    return Boolean(AppState.token && AppState.user);
  },

  async login() {
    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value;
    const errorBox = document.getElementById('login-error');
    errorBox.classList.add('hidden');

    try {
      const data = await Api.post('/auth/login', { email, password });
      this.saveSession(data);
      Toast.show('Вход выполнен.');
      Router.go('boards');
    } catch (error) {
      errorBox.textContent = error.message;
      errorBox.classList.remove('hidden');
    }
  },

  async register() {
    const userName = document.getElementById('reg-username').value.trim();
    const email = document.getElementById('reg-email').value.trim();
    const password = document.getElementById('reg-password').value;

    const errorBox = document.getElementById('reg-error');
    errorBox.classList.add('hidden');

    try {
      const data = await Api.post('/auth/register', { userName, email, password});
      this.saveSession(data);
      Toast.show('Аккаунт создан.');
      Router.go('boards');
    } catch (error) {
      errorBox.textContent = error.message;
      errorBox.classList.remove('hidden');
    }
  },

  saveSession(data) {
    AppState.token = data.token;
    AppState.user = {
      id: data.userId,
      userName: data.userName,
      email: data.email,
      role: data.role
    };
    localStorage.setItem('taskboard_token', AppState.token);
    localStorage.setItem('taskboard_user', JSON.stringify(AppState.user));
    this.renderUser();
  },

  logout(showMessage = true) {
    AppState.token = null;
    AppState.user = null;
    localStorage.removeItem('taskboard_token');
    localStorage.removeItem('taskboard_user');
    if (showMessage) {
      Toast.show('Вы вышли из аккаунта.');
    }
    Router.go('login');
  },

  async refreshProfile() {
    try {
      const profile = await Api.get('/auth/profile');
      AppState.user = {
        id: profile.id,
        userName: profile.userName,
        email: profile.email,
        role: profile.role
      };
      localStorage.setItem('taskboard_user', JSON.stringify(AppState.user));
      this.renderUser();
    } catch (error) {
      Toast.error(error);
    }
  },

  renderUser() {
    const user = AppState.user;
    if (!user) {
      return;
    }

    const letter = user.userName?.[0]?.toUpperCase() || '?';
    document.getElementById('sidebar-avatar').textContent = letter;
    document.getElementById('sidebar-name').textContent = user.userName;
    document.getElementById('sidebar-role').textContent = user.role;
    document.getElementById('profile-avatar-big').textContent = letter;
    document.getElementById('profile-name').textContent = user.userName;
    document.getElementById('profile-email').textContent = user.email;
    document.getElementById('profile-role').textContent = user.role;
  }
};
