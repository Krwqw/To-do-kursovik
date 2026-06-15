const Modal = {
  confirmAction: null,

  open(id) {
    document.getElementById(id)?.classList.remove('hidden');
  },

  close(id) {
    document.getElementById(id)?.classList.add('hidden');
  },

  closeOnOverlay(event, id) {
    if (event.target.id === id) {
      this.close(id);
    }
  },

  confirm(title, text, action) {
    document.getElementById('confirm-title').textContent = title;
    document.getElementById('confirm-text').textContent = text;
    this.confirmAction = action;
    document.getElementById('confirm-btn').onclick = async () => {
      if (this.confirmAction) {
        await this.confirmAction();
      }
      this.close('modal-confirm');
    };
    this.open('modal-confirm');
  }
};
