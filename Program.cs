using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var threads = new List<Thread>();
var running = true;
var meetingId = Environment.GetEnvironmentVariable("MEETING_ID");
var password = Environment.GetEnvironmentVariable("MEETING_PASSWORD");
var threadCount = int.Parse(Environment.GetEnvironmentVariable("PARTICIPANT_COUNT") ?? "0");

Console.WriteLine("MS Teams Dummy Participant Runner - Using Chrome");
Console.WriteLine("Created by Elias Puurunen @ Tractus Events - https://www.tractusevents.com");

if (string.IsNullOrEmpty(meetingId) || string.IsNullOrEmpty(password) || threadCount <= 0)
{
    Console.WriteLine("Please provide the Teams meeting ID, password, and number of participants.");
    Environment.Exit(1); // Exit with an error if required values are not provided.
}

for (var i = 0; i < threadCount; i++)
{
    var thread = new Thread((o) =>
    {
        var participantNumber = (int)o;
        var chromeOptions = new ChromeOptions
        {
            // Use headless mode
            AddArgument("--headless"),
            AddArgument("--window-size=1280,720"),
            AddArgument("--mute-audio"),
            AddArgument("--ignore-certificate-errors"),
            AddArgument("--disable-extensions"),
            AddArgument("--no-sandbox"),
            AddArgument("--disable-dev-shm-usage"),
            AddArgument("--use-fake-device-for-media-stream"),
            AddArgument("--use-fake-ui-for-media-stream"),
            AddArgument("--log-level=3"),
            AddArgument("--disable-notifications"),
            AddArgument("--disable-popup-window")
        };

        using var driver = new ChromeDriver(chromeOptions);

        driver.Navigate().GoToUrl($"https://teams.microsoft.com/v2/?meetingjoin=true#/meet/{meetingId.Replace(" ", "")}?launchAgent=marketing_join&laentry=hero&p={password}&anon=true&deeplinkId=251e9ce4-ef63-44dd-9115-a2d4b9c4f46d");

        while (true)
        {
            var input = driver.FindElements(By.TagName("input")).FirstOrDefault();

            if (input is null)
            {
                Thread.Sleep(250);
                continue;
            }

            input.SendKeys($"Test Participant {participantNumber}");
            break;
        }

        while (true)
        {
            var button = driver.FindElements(By.TagName("button")).FirstOrDefault(x => x.Text.Contains("join now", StringComparison.InvariantCultureIgnoreCase));

            if (button is null)
            {
                Thread.Sleep(250);
                continue;
            }

            button.Click();
            break;
        }

        while (true)
        {
            try
            {
                var button = driver.FindElements(By.TagName("button")).FirstOrDefault(x => x.GetDomAttribute("id").Contains("microphone-button", StringComparison.InvariantCultureIgnoreCase));

                if (button is null)
                {
                    Thread.Sleep(250);
                    continue;
                }

                button.Click();
                break;
            }
            catch
            {
                Thread.Sleep(250);
            }
        }

        while (running)
        {
            Thread.Sleep(250);
        }

        var hangup = driver.FindElements(By.TagName("button"))
            .FirstOrDefault(x => x.GetDomAttribute("id") == "hangup-button");

        hangup?.Click();

        Thread.Sleep(3000);
        driver.Close();

    });

    threads.Add(thread);

    thread.Start(i + 1);
}

Console.WriteLine("Launched. Type q and hit enter to exit.");
while (true)
{
    var command = Console.ReadLine();
    if (command.Contains("q", StringComparison.InvariantCultureIgnoreCase))
    {
        running = false;
        break;
    }
}

Console.WriteLine("Exiting...");
for (var i = 0; i < threads.Count; i++)
{
    threads[i].Join();
}

Console.WriteLine("All threads finished. Exiting the app.");
