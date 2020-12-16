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
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Terminal.Gui;
using System.Net.Cache;
using System.Timers;
using System.Threading;
namespace Client
{
    class Program

    {

        private static MenuBar menu;
        private static Window winMain;

        private static Window winMessages;

        private static Label labelUsername;

        private static Label labelMessage;
        private static TextField fieldUsername;

        private static TextField fieldMessage;

        private static Button btnSend;

        private static List<Message> messages = new List<Message>();

        static void Main(string[] args)
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
        
            Application.Init();
            //
            //ColorScheme colorDark = new ColorScheme();

            //colorDark.Normal = new Terminal.Gui.Attribute(Color.White, Color.DarkGray);
            //
            //	Создание верхнего меню приложения 
            menu = new MenuBar(new MenuBarItem[] {

            new MenuBarItem("_App", new MenuItem[] { new MenuItem("_Quit", "Close the app", Application.RequestStop),}),}) 
            { 
                X=0,Y=0,
                Width = Dim.Fill(), Height = 1,
            };
    Application.Top.Add(menu);

//	Создание главного окна
winMain = new Window()
    {

        X = 0,

Y = 1,
Width = Dim.Fill(),
 
Height = Dim.Fill(),

Title = "DotChat",

};
    //winMain.ColorScheme = colorDark;

    Application.Top.Add(winMain);

//	Создание окна с сообщениями 
            winMessages = new Window() { X=0, Y=0,
 Width = winMain.Width, Height = winMain.Height - 2, };
winMain.Add(winMessages);

//	Создание надписи с username 
            labelUsername = new Label() {
                X = 0,
                Y = Pos.Bottom(winMain) - 5, Width = 15,
                Height = 1,
                Text = "Username:",

TextAlignment = TextAlignment.Right,
};
winMain.Add(labelUsername);

//	Создание надписи с message 
            labelMessage = new Label()

{
    X = 0,

Y = Pos.Bottom(winMain) - 4, Width = 15,
Height = 1,
Text = "Message:",

TextAlignment = TextAlignment.Right,
};
winMain.Add(labelMessage);

//	Создание поля ввода username 
            fieldUsername = new TextField()

{
    X = 15,

Y = Pos.Bottom(winMain) - 5, Width = winMain.Width - 15, Height = 1,

};
winMain.Add(fieldUsername);

//	Создание поля ввода message 
            fieldMessage = new TextField()

{
    X = 15,

Y = Pos.Bottom(winMain) - 4, Width = winMain.Width - 15, Height = 1,

};
winMain.Add(fieldMessage);

//	Создание кнопки отправки
btnSend = new Button()

{

    X = Pos.Right(winMain) - 15,
    Y = Pos.Bottom(winMain) - 4,

    Width = 15,

    Height = 1,

    Text = "	SEND	",

};

btnSend.Clicked += OnBtnSendClick;
winMain.Add(btnSend);

//	Создание цикла получения сообщений 
            int lastMsgID = 0;
            System.Timers.Timer updateLoop = new System.Timers.Timer();
            updateLoop.Interval = 1000;

updateLoop.Elapsed += (object sender, ElapsedEventArgs e) => {
    Message msg = GetMessage(lastMsgID);

    if (msg != null)
    {
        messages.Add(msg); MessagesUpdate(); lastMsgID++;
    }
};

updateLoop.Start();

Application.Run();

}

//	Реакция на клик кнопки 
        static void OnBtnSendClick() {

if (fieldUsername.Text.Length != 0 && fieldMessage.Text.Length != 0)
{
    Message msg = new Message()
    {

        username = fieldUsername.Text.ToString(),
        text = fieldMessage.Text.ToString(),

    };

    SendMessage(msg); fieldMessage.Text = "";

}
}

//	Синхронизирует список сообщений с представлением

static void MessagesUpdate()
{

    //winMessages.RemoveAll();
    int offset = 0;

    for (var i = messages.Count - 1; i >= 0; i--)
    {
        View msg = new View()
        {
            X = 0,
            Y = offset,

            Width = winMessages.Width,

            Height = 1,

            Text = $"[{messages[i].username}] {messages[i].text}",
        };

        winMessages.Add(msg);

        offset++;
    }

    Application.Refresh();

}

//	Отправляет сообщение на сервер 
        static void SendMessage(Message msg) {

WebRequest req = WebRequest.Create("http://localhost:5000/api/chat"); req.Method = "POST";

string postData = JsonConvert.SerializeObject(msg); byte[] bytes = Encoding.UTF8.GetBytes(postData); req.ContentType = "application/json"; req.ContentLength = bytes.Length;

Stream reqStream = req.GetRequestStream(); reqStream.Write(bytes); reqStream.Close();

req.GetResponse();

}

//	Получает сообщение с сервера 
        static Message GetMessage(int id) {

WebRequest req = WebRequest.Create($"http://localhost:5000/api/chat/{id}"); WebResponse resp = req.GetResponse();

string smsg = new StreamReader(resp.GetResponseStream()).ReadToEnd();

if (smsg == "Not found") return null;

return JsonConvert.DeserializeObject<Message>(smsg);

}
        private static void Login()

        {

            do

            {

                //HttpWebRequest httpWebRequest = (HttpWebRequest)

                //WebRequest.Create($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Login");
                Uri httpWebRequest = new Uri($"http://{ConfigManager.Config.IP}:{ConfigManager.Config.Port}/api/Login");

                WebRequest wr = WebRequest.Create(httpWebRequest);

                wr.ContentType = "application/json"; wr.Method = "POST";

                string result;

                var regData = GetRegData((HttpWebRequest)wr);

                try
                {

                    var httpResponse = (HttpWebResponse)wr.GetResponse(); var streamReader =

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
