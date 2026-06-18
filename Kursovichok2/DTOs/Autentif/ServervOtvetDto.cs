namespace Kursovichok2.DTOs.Autentif
{
    public class ServervOtvetDto
    {
        //то что получает фронт от сервера
        public string Token { get; set; } = string.Empty; /*JWT-токен - компактный токен для аутентификации и авторизации, 
                                                            позволяет получать доступ к защищенным API-эндпоинтам
                                                            без хранения сессий на сервере*/
        public int UserId { get; set; } //айди пользователя
        public string UserName { get; set; } = string.Empty; //имя пользователя
        public string Email { get; set; } = string.Empty; //почту
        public string Role {  get; set; } = string.Empty; //роль
    }
}
