using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace Client
{

    ///	<summary>
    ///	Класс хранения, сохранения и загрузки конфигурации
    ///	</summary>
    internal class ConfigManager

    {

        ///	<summary>
        ///	Путь к файлу конфигурации
        ///	</summary>

        private const string Path = @"config.json";

        ///	<summary>
        ///	Текущие настройки клиента
        ///	</summary>
        public static Config Config = new Config();

        ///	<summary>
        ///	Запись настроек в файл
        ///	</summary>

        public static async void WriteConfig()
        {

            await using var streamWriter = new StreamWriter(Path);

            await streamWriter.WriteAsync(JsonConvert.SerializeObject(Config));
        }

        ///	<summary>
        ///	Загрузка настроек из файла
        ///	</summary>

        public static async void LoadConfig()

        {
            if (!File.Exists(Path)) WriteConfig();

            using var streamReader = new StreamReader(Path); Config = JsonConvert.DeserializeObject<Config>(await

            streamReader.ReadToEndAsync());

        }

    }

    ///	<summary>
    ///	Класс настроек

    ///	</summary> 
    internal class Config

    {

        ///	<summary>

        ///	MillisecondsSleep - время обновления истории сообщений
        ///	</summary>

        public int MillisecondsSleep { get; set; } = 200;

        ///	<summary>
        ///	Логин пароль пользователя
        ///	</summary>

        public RegData RegData { get; set; } = new RegData { Username = "Anonymous", Password = "password" };

        ///	<summary>
        ///	Токен необходимый для отправки сообщений
        ///	</summary>
        public string Token { get; set; }

        ///	<summary>
        ///	Адрес сервера

        ///	</summary>

        public string IP { get; set; } = "localhost";

        ///	<summary>
        ///	Порт сервера
        ///	</summary>
        public int Port { get; set; } = 5000;

    }

    ///	<summary>
    ///	Класс для хранения связки логин/пароль
    ///	</summary>
    public class RegData

    {

        ///	<summary>
        ///	Логин уникальный псевдоним пользователя
        ///	</summary>

        public string Username { get; set; }

        ///	<summary>
        ///	Пароль - необходим для доступа
        ///	</summary>
        public string Password { get; set; }

    }
}
