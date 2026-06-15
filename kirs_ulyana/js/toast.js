const Toast = {
  show(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(20px)';
      setTimeout(() => toast.remove(), 180);
    }, 2800);
  },

  error(error) {
    this.show(error?.message || 'Что-то пошло не так.', 'error');
  }
};
