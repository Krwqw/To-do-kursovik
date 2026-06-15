document.addEventListener('DOMContentLoaded', () => {
  Auth.init();
  Auth.renderUser();

  const credit = document.getElementById('course-author');
  if (credit) {
    credit.textContent = `${AppConfig.author}, группа ${AppConfig.group}`;
  }

  Router.go(Auth.isLoggedIn() ? 'boards' : 'login');
});
