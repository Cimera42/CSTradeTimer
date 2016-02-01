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
			string password = System.IO.File.ReadAllText(@"../cstrade_admin_password.txt");
			var passworddata = Encoding.ASCII.GetBytes ("password=" + password);

			bool somethingFailed = false;
			while(somethingFailed == false)
			{
				string startUrl = "http://skinbonanza.com/backend/new_pot.php";
				HttpWebRequest startRequest = (HttpWebRequest) WebRequest.Create (startUrl);

				startRequest.Method = "POST";
				startRequest.ContentType = "application/x-www-form-urlencoded";
				startRequest.ContentLength = passworddata.Length;

				using (var stream = startRequest.GetRequestStream ()) {
					stream.Write (passworddata, 0, passworddata.Length);
				}

				TimerData data = new TimerData();
				bool startSucceeded = false;
				for(int attemptNum = 0; attemptNum < 5; attemptNum++)
				{
					try {
						HttpWebResponse startResponse = (HttpWebResponse)startRequest.GetResponse ();
						string startString = new StreamReader (startResponse.GetResponseStream ()).ReadToEnd ();
						Console.WriteLine ("str: " + startString);
						data = JsonConvert.DeserializeObject<TimerData> (startString);
						Console.WriteLine ("Timer started: " + data.timer_start + " - " + data.timer_length);
						startSucceeded = true;
						break;
					} catch (Exception e) {
						Console.WriteLine (e.Message);
					}
				}

				if (startSucceeded) {
					int timerLength = data.timer_length * 1000;
					int sleepTime = timerLength * 3 / 4;
					Console.WriteLine ("Timer sleep " + sleepTime);
					Thread.Sleep (sleepTime);
					Console.WriteLine ("Timer sleep ended");

					long timerEnd = data.timer_start + timerLength;
					TimeSpan now = (DateTime.UtcNow - new DateTime (1970, 1, 1));
					while (now.TotalMilliseconds < timerEnd) {
						now = (DateTime.UtcNow - new DateTime (1970, 1, 1));
					}
					Console.WriteLine ("Timer ended");

					string endUrl = "http://skinbonanza.com/backend/end_timer.php";
					HttpWebRequest endRequest = (HttpWebRequest)WebRequest.Create (endUrl);

					endRequest.Method = "POST";
					endRequest.ContentType = "application/x-www-form-urlencoded";
					endRequest.ContentLength = passworddata.Length;

					using (var stream = endRequest.GetRequestStream ()) {
						stream.Write (passworddata, 0, passworddata.Length);
					}

					HttpWebResponse endResponse = (HttpWebResponse)endRequest.GetResponse ();
					string endString = new StreamReader (endResponse.GetResponseStream ()).ReadToEnd ();
					Console.WriteLine ("estr: " + endString);
					Console.WriteLine ("Timer end message sent");

					string processUrl = "http://skinbonanza.com/backend/choose_winner.php";
					HttpWebRequest processRequest = (HttpWebRequest)WebRequest.Create (processUrl);

					processRequest.Method = "POST";
					processRequest.ContentType = "application/x-www-form-urlencoded";
					processRequest.ContentLength = passworddata.Length;

					using (var stream = processRequest.GetRequestStream ()) {
						stream.Write (passworddata, 0, passworddata.Length);
					}

					bool processSucceeded = false;
					for (int attemptNum = 0; attemptNum < 5; attemptNum++) {
						try {
							HttpWebResponse processResponse = (HttpWebResponse)processRequest.GetResponse ();
							string processString = new StreamReader (processResponse.GetResponseStream ()).ReadToEnd ();

							Console.WriteLine ("pstr: " + processString);
							if(processString.Contains("success"))
							{
								Console.WriteLine ("Pot processed");
								processSucceeded = true;
								break;
							}
						} catch (Exception e) {
							Console.WriteLine (e.Message);
						}
					}
					if (processSucceeded == false) {
						Console.WriteLine ("####################");
						Console.WriteLine ("CATASTROPHIC FAILURE");
						Console.WriteLine ("####################");
						somethingFailed = true;
					}

					Thread.Sleep (3000);
				}
				else
				{
					somethingFailed = true;
				}
			}
		}
	}
}
