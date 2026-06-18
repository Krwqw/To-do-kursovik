var Boards = {
    items: [],
    editingBoardId: null,

    async load() {
        const grid = document.getElementById('boards-grid');
        grid.innerHTML = `
      <div class="skeleton-grid">
        <div class="skeleton-card"></div>
        <div class="skeleton-card"></div>
        <div class="skeleton-card"></div>
      </div>
    `;

        try {
            this.items = await Api.get('/boards');
            this.render();
        } catch (error) {
            grid.innerHTML = `<div class="empty-state"><h3>Не удалось загрузить доски</h3><p>${escapeHtml(error.message)}</p></div>`;
        }
    },

    render() {
        const grid = document.getElementById('boards-grid');
        if (!this.items.length) {
            grid.innerHTML = `
        <div class="empty-state">
          <h3>Досок пока нет</h3>
          <p>Создайте первую доску для задач курсового проекта.</p>
        </div>
      `;
            return;
        }

        grid.innerHTML = this.items.map((board) => `
      <article class="board-card" onclick="Router.go('board', { id: ${board.id} })">
        <h3 class="board-card-title">${escapeHtml(board.title)}</h3>
        <p class="board-card-desc">${escapeHtml(board.description || 'Без описания')}</p>
        <div class="board-card-footer">
          <span class="board-card-date">${formatDate(board.createdAt)}</span>
          <span class="board-card-actions">
            <button class="btn-icon" title="Редактировать" onclick="event.stopPropagation(); Boards.openEditModal(${board.id})">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"/></svg>
            </button>
            <button class="btn-icon" title="Удалить" onclick="event.stopPropagation(); Boards.deleteBoard(${board.id})">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M8 6V4h8v2"/><path d="M19 6l-1 14H6L5 6"/></svg>
            </button>
          </span>
        </div>
      </article>
    `).join('');
    },

    openCreateModal() {
        this.editingBoardId = null;
        document.getElementById('modal-board-title').textContent = 'Новая доска';
        document.getElementById('board-name-input').value = '';
        document.getElementById('board-desc-input').value = '';
        document.getElementById('board-submit-btn').textContent = 'Создать';
        Modal.open('modal-board');
    },

    openEditModal(id) {
        // Ищем доску в списке или в текущем состоянии
        const board = this.items.find((item) => item.id === id) || AppState.currentBoard;

        if (!board) {
            return;
        }

        this.editingBoardId = board.id;
        document.getElementById('modal-board-title').textContent = 'Редактировать доску';
        document.getElementById('board-name-input').value = board.title;
        document.getElementById('board-desc-input').value = board.description || '';
        document.getElementById('board-submit-btn').textContent = 'Сохранить';
        Modal.open('modal-board');
    },

    async submitBoard() {
        const title = document.getElementById('board-name-input').value.trim();
        const description = document.getElementById('board-desc-input').value.trim();

        if (!title) {
            Toast.show('Введите название доски.', 'error');
            return;
        }

        try {
            if (this.editingBoardId) {
                // 🔹 РЕДАКТИРОВАНИЕ

                // Внимание: Бэкенд возвращает 204 No Content (пустоту).
                // Поэтому мы НЕ присваиваем результат переменной (await Api.put(...)).
                await Api.put(`/boards/${this.editingBoardId}`, { title, description });

                // Вместо этого обновляем данные вручную в массиве
                const board = this.items.find((item) => item.id === this.editingBoardId);
                if (board) {
                    board.title = title;
                    board.description = description;
                }

                // Если мы находимся внутри этой доски, обновляем и AppState
                if (AppState.currentBoard?.id === this.editingBoardId) {
                    AppState.currentBoard.title = title;
                    AppState.currentBoard.description = description;
                    // Обновляем заголовок, если функция доступна
                    if (typeof Board !== 'undefined') Board.renderHeader(AppState.currentBoard);
                }

                Toast.show('Доска обновлена.');

            } else {
                //  СОЗДАНИЕ НОВОЙ

                // При создании (POST) бэкенд возвращает объект (201 Created), так что тут всё ок.
                const created = await Api.post('/boards', { title, description });
                this.items.push(created); // Добавляем новую доску в конец списка
                Toast.show('Доска создана.');
            }

            Modal.close('modal-board');
            this.render(); // Перерисовываем список

        } catch (error) {
            Toast.error(error);
        }
    },

    async deleteBoard(id) {
        Modal.confirm('Удалить доску?', 'Все задачи на этой доске тоже будут удалены.', async () => {
            try {
                // Удаление тоже возвращает 204, поэтому просто ждем завершения запроса
                await Api.delete(`/boards/${id}`);

                this.items = this.items.filter((board) => board.id !== id);
                Toast.show('Доска удалена.');
                this.render();
            } catch (error) {
                Toast.error(error);
            }
        });
    }
};