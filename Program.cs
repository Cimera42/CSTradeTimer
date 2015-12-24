using System;
using System.Net;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Text;

namespace CSTradeTimer
{
	class MainClass
	{
		class TimerData
		{
			public long timer_start = 0;
			public int timer_length = 0;
		}

		public static void Main (string[] args)
		{
			string password = System.IO.File.ReadAllText(@"E:\Programming\Web\cstrade_admin_password.txt");
			var passworddata = Encoding.ASCII.GetBytes ("password=" + password);
			while(true)
			{
				string startUrl = "http://10.0.0.53:4001/php/backend/new_pot.php";
				HttpWebRequest startRequest = (HttpWebRequest) WebRequest.Create (startUrl);

				startRequest.Method = "POST";
				startRequest.ContentType = "application/x-www-form-urlencoded";
				startRequest.ContentLength = passworddata.Length;

				using (var stream = startRequest.GetRequestStream ()) {
					stream.Write (passworddata, 0, passworddata.Length);
				}

				HttpWebResponse startResponse = (HttpWebResponse) startRequest.GetResponse ();
				string startString = new StreamReader (startResponse.GetResponseStream()).ReadToEnd();
				Console.WriteLine("str: " + startString);
				TimerData data = JsonConvert.DeserializeObject<TimerData> (startString);
				Console.WriteLine("Timer started: " + data.timer_start + " - " + data.timer_length);

				int timerLength = data.timer_length * 1000;
				int sleepTime = timerLength*3/4;
				Console.WriteLine("Timer sleep " + sleepTime);
				Thread.Sleep(sleepTime);
				Console.WriteLine("Timer sleep ended");

				long timerEnd = data.timer_start + timerLength;
				TimeSpan now = (DateTime.UtcNow - new DateTime(1970, 1, 1));
				while(now.TotalMilliseconds < timerEnd)
				{
					now = (DateTime.UtcNow - new DateTime(1970, 1, 1));
				}
				Console.WriteLine("Timer ended");

				string endUrl = "http://10.0.0.53:4001/php/backend/end_timer.php";
				HttpWebRequest endRequest = (HttpWebRequest) WebRequest.Create (endUrl);

				endRequest.Method = "POST";
				endRequest.ContentType = "application/x-www-form-urlencoded";
				endRequest.ContentLength = passworddata.Length;

				using (var stream = endRequest.GetRequestStream ()) {
					stream.Write (passworddata, 0, passworddata.Length);
				}
				HttpWebResponse endResponse = (HttpWebResponse) endRequest.GetResponse ();
				string endString = new StreamReader (endResponse.GetResponseStream()).ReadToEnd();
				Console.WriteLine("estr: " + endString);
				Console.WriteLine("Timer end message sent");

				string processUrl = "http://10.0.0.53:4001/php/backend/choose_winner.php";
				HttpWebRequest processRequest = (HttpWebRequest) WebRequest.Create (processUrl);

				processRequest.Method = "POST";
				processRequest.ContentType = "application/x-www-form-urlencoded";
				processRequest.ContentLength = passworddata.Length;

				using (var stream = processRequest.GetRequestStream ()) {
					stream.Write (passworddata, 0, passworddata.Length);
				}
				HttpWebResponse processResponse = (HttpWebResponse) processRequest.GetResponse ();
				string processString = new StreamReader (processResponse.GetResponseStream()).ReadToEnd();
				Console.WriteLine("pstr: " + processString);
				Console.WriteLine("Pot processed");

				Thread.Sleep(3000);
			}
		}
	}
}
