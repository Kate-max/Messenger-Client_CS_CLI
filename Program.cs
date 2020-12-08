using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System.IO;

using System.Net;

using System.Threading;



using Newtonsoft.Json;



using System.Text;
//using Microsoft.AspNetCore.Authentication.JwtBearer3.1;
//using Microsoft.IdentityModel.Tokens;
namespace Client
{

    internal class Program

    {
        ///	<summary>
        ///	Точка входа
        ///	</summary>
        private static void Main()

        {

            ConfigManager.LoadConfig();

            string IP;

            string port;

            Console.Write("Enter IP(or press enter or default):"); IP = Console.ReadLine();

            if (!string.IsNullOrEmpty(IP))

            {
                Console.Write("Enter port:");

                port = Console.ReadLine();

                ConfigManager.Config.IP = IP;

                if (int.TryParse(port, out var configPort))
                    ConfigManager.Config.Port = configPort;

                else

                    Console.WriteLine($"Wrong port. Use {configPort}");
            }

            Login();

            var onlineUpdaterThread = new Thread(ServerResponse.OnlineUpdater) { Name = "OnlineUpdaterThread" };

            onlineUpdaterThread.Start();

            var historyUpdaterThread = new Thread(ServerResponse.GetHistoryMessages) { Name = "HistoryUpdaterThread" };

            historyUpdaterThread.Start();

            while (true)

                try
                {

                    while (true) Post();

                }
                catch (Exception)

                {

                    // ignored

                }
        }

        ///	<summary>
        ///	Запрос у пользователя уникального ника/пароля
        ///	</summary>

        private static void Login()

        {

            do

            {
                var httpWebRequest = (HttpWebRequest)

                WebRequest.Create($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Login");


                httpWebRequest.ContentType = "application/json"; httpWebRequest.Method = "POST";

                string result;

                var regData = GetRegData(httpWebRequest);

                try
                {

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse(); var streamReader =

                   new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException());

                    result = streamReader.ReadToEnd();
                }

                catch (Exception)

                {

                    Console.SetCursorPosition(0, 0);
                    continue;

                }

                var temp = JsonConvert.DeserializeAnonymousType(result, new
                {
                    Token =

                ""
                });

                ConfigManager.Config.Token = temp.Token;

                ConfigManager.Config.RegData = regData;
                Console.WriteLine("Success!");

                ConfigManager.WriteConfig();

                break;

            } while (true);
        }

        ///	<summary>
        ///	Запрос на уникальность ника
        ///	</summary>
        ///	<param name="httpWebRequest">Веб запрос</param>
        ///	<returns>Связка Логин пароль</returns>

        private static RegData GetRegData(HttpWebRequest httpWebRequest)

        {

            Console.Write("Enter your nick> ".PadRight(Console.BufferWidth - 1));
            Console.Write("".PadRight(Console.BufferWidth - 1));

            Console.SetCursorPosition(0, 0);

            Console.Write("Enter your nick> ");
            var nick = Console.ReadLine();

            Console.Write("Enter your password> ");

            var password = Console.ReadLine();

            var regData = new RegData { Username = nick, Password = password }; var json = JsonConvert.SerializeObject(regData);
            var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());

            streamWriter.Write(json);

            streamWriter.Close();

            return regData;

        }

        ///	<summary>

        ///	<para>Функция ввода и печати сообщения в час (отправляется POST запрос на сервер)</para>

        /// </summary>

        private static async void Post()

        {


            Console.Write("Enter message(or /u for update)>                \b\b\b\b\b\b\b");
            var msg = Console.ReadLine();
            if (msg.Equals("/update") || msg.Equals("/u"))
            {



                await ServerResponse.UpdateHistory(); Console.SetCursorPosition(0, Console.CursorTop - 1); return;

            }

            var httpWebRequest = (HttpWebRequest)
            WebRequest.Create($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Chat");


            httpWebRequest.ContentType = "application/json"; httpWebRequest.Method = "POST"; httpWebRequest.Headers.Add("Authorization", "Bearer " +

            ConfigManager.Config.Token);

            SendMessage(msg, httpWebRequest);

            GetAnswer(httpWebRequest);

        }

        ///	<summary>
        ///	Функция получения ответа от сервер после отправки сообщения
        ///	</summary>
        ///	<param name="httpWebRequest">Веб запрос</param>

        private static void GetAnswer(HttpWebRequest httpWebRequest)
        {

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using var streamReader = new StreamReader(httpResponse.GetResponseStream()); var result = streamReader.ReadToEnd();

            if (result != "ok") Console.WriteLine("Something went wrong");

        }

        ///	<summary>
        ///	Функция отправки сообщения на сервер
        ///	</summary>
        ///	<param name="msg">Сообщение</param>
        ///	<param name="httpWebRequest">Веб запрос</param>

        private static void SendMessage(string msg, HttpWebRequest httpWebRequest)
        {

            var json = JsonConvert.SerializeObject(new Message { Text = msg });

            using var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());

            streamWriter.Write(json);
            streamWriter.Close();

        }

    }

}





namespace Client

{
    internal class ServerResponse

    {

        ///	<summary>
        ///	Предыдущая длина списка сообщений

        /// </summary>

        private static int _len;

        ///	<summary>
        ///	Отправка сигнала о том что пользователь онлайн
        ///	</summary>
        private static async Task PostOnline()

        {

            try
            {

                var httpWebRequest =

                (HttpWebRequest)

                WebRequest.Create($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Onli ne");

                httpWebRequest.ContentType = "application/json"; httpWebRequest.Method = "POST"; httpWebRequest.Headers.Add("Authorization", "Bearer " +

                ConfigManager.Config.Token);

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                var streamReader = new StreamReader(httpResponse.GetResponseStream()); var result = await streamReader.ReadToEndAsync();

                if (result != "ok") throw new Exception("Something went wrong");

            }
            catch (Exception)

            {

                // ignored
            }

        }

        ///	<summary>
        ///	Цикл отправки сигнала что пользователь онлайн

        ///	</summary>
        public static async void OnlineUpdater()

        {

            while (true)

                try
                {

                    await PostOnline();

                    Thread.Sleep(500);
                }

                catch (Exception)

                {
                    // ignored

                }

        }

        ///	<summary>
        ///	Получает ответ на GET запрос
        ///	</summary>
        ///	<param name="uri">Ссылка на HTTP</param>
        ///	<returns>Ответ на GET запрос</returns>

        public static async Task<string> GetAsync(string uri)
        {

            var request = (HttpWebRequest)WebRequest.Create(uri); request.AutomaticDecompression = DecompressionMethods.GZip |
           DecompressionMethods.Deflate;

            using var response = (HttpWebResponse)await request.GetResponseAsync(); await using var stream = response.GetResponseStream(); using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();

        }

        ///	<summary>
        ///	Цикл запроса обновления истории сообщений

        /// </summary>

        public static async void GetHistoryMessages()

        {
            while (true)

            {

                await UpdateHistory();

                Thread.Sleep(ConfigManager.Config.MillisecondsSleep);

            }
        }

        ///	<summary>
        ///	Функция обновления сообщений
        ///	</summary>

        public static async Task UpdateHistory()

        {
            var res = await

            GetAsync($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Chat");

            if (res != "[]")

            {

                var messages = JsonConvert.DeserializeObject<List<Message>>(res);

                if (_len != messages.Count)

                {

                    var x = Console.CursorLeft;
                    var y = Console.CursorTop;

                    Console.MoveBufferArea(0, y, x, 1, 0, messages.Count + 1);

                    var history = "";

                    foreach (var message in messages)
                        history += message.ToString().PadRight(Console.BufferWidth - 1) +

                        "\n";

                    Console.SetCursorPosition(0, 1); Console.WriteLine(history); Console.SetCursorPosition(x, messages.Count + 1);

                    _len = messages.Count;
                }

            }

        }
    }

}



namespace Client
{

    ///	<summary>
    ///	<para>Класс Сообщение</para>
    ///	</summary>

    public class Message

    {
        ///	<summary>
        ///	<br>Ts - время отправки сообщения (по серверу)</br>
        ///	</summary>

        public int Ts { get; set; }

        ///	<summary>
        ///	<br>Name - имя клиента</br>
        ///	</summary>

        public string Name { get; set; }

        /// <summary>

        ///	<br>Text - сообщение клиента</br>
        ///	</summary>

        public string Text { get; set; }

        ///	<summary>
        ///	<br>ToString - функция преобразования полей класса в строку для печати</br>
        ///	</summary>
        ///	<returns> [Time] Name: Text </returns>
        public override string ToString()

        {

            return $"[{new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Ts)}] {Name}:{ Text}";
        }

    }

}



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




