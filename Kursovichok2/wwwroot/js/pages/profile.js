var Profile = {
    async load() {
        try {
            const user = await Api.get('/auth/profile');

            document.getElementById('profile-name').textContent = user.userName;
            document.getElementById('profile-email').textContent = user.email;

            const roleMap = {  'user': 'Пользователь' };
            document.getElementById('profile-role').textContent = roleMap[user.role] || user.role;

            document.getElementById('profile-avatar-big').textContent = (user.userName || '?')[0].toUpperCase();

        } catch (error) {
            Toast.error('Не удалось загрузить профиль');
            Router.go('login');
        }
    }
};