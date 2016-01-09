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
			string password = System.IO.File.ReadAllText(@"F:\Stuff\Websites\cstrade_admin_password.txt");
			var passworddata = Encoding.ASCII.GetBytes ("password=" + password);
			var updatePricePer = 240; //Update prices every X pots (currently every 2 hours (30sec pots))
			var updatePriceCount = 239; //Update at start of program
			while(true)
			{
				updatePriceCount++;
				string startUrl = "http://127.0.0.1:7001/backend/new_pot.php";
				HttpWebRequest startRequest = (HttpWebRequest) WebRequest.Create (startUrl);

				startRequest.Method = "POST";
				startRequest.ContentType = "application/x-www-form-urlencoded";
				startRequest.ContentLength = passworddata.Length;

				using (var stream = startRequest.GetRequestStream ()) {
					stream.Write (passworddata, 0, passworddata.Length);
				}

				TimerData data = new TimerData();
				bool startSucceeded = false;
				for(int attemptNum = 0; attemptNum < 10; attemptNum++)
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

					string endUrl = "http://127.0.0.1:7001/backend/end_timer.php";
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

					string processUrl = "http://127.0.0.1:7001/backend/choose_winner.php";
					HttpWebRequest processRequest = (HttpWebRequest)WebRequest.Create (processUrl);

					processRequest.Method = "POST";
					processRequest.ContentType = "application/x-www-form-urlencoded";
					processRequest.ContentLength = passworddata.Length;

					using (var stream = processRequest.GetRequestStream ()) {
						stream.Write (passworddata, 0, passworddata.Length);
					}

					bool processSucceeded = false;
					for (int attemptNum = 0; attemptNum < 10; attemptNum++) {
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
					}

					Thread.Sleep (3000);

					if (updatePriceCount == updatePricePer) {
						updatePriceCount = 0;

						System.Diagnostics.Process proc = new System.Diagnostics.Process ();
						proc.StartInfo.FileName = "F:\\Stuff\\Websites\\CSTrade\\trunk\\updatePrices.bat";
						proc.StartInfo.WorkingDirectory = "F:\\Stuff\\Websites\\CSTrade\\trunk\\";
						proc.Start ();
					}
				}
			}
		}
	}
}
