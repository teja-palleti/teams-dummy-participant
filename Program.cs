using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        if (o is not int participantNumber) return; // Safe null-check for unboxing

        var chromeOptions = new ChromeOptions();

        // Add arguments one by one
        chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--window-size=1280,720");
        chromeOptions.AddArgument("--mute-audio");
        chromeOptions.AddArgument("--ignore-certificate-errors");
        chromeOptions.AddArgument("--disable-extensions");
        chromeOptions.AddArgument("--no-sandbox");
        chromeOptions.AddArgument("--disable-dev-shm-usage");
        chromeOptions.AddArgument("--use-fake-device-for-media-stream");
        chromeOptions.AddArgument("--use-fake-ui-for-media-stream");
        chromeOptions.AddArgument("--log-level=3");
        chromeOptions.AddArgument("--disable-notifications");
        chromeOptions.AddArgument("--disable-popup-window");

        using var driver = new ChromeDriver(chromeOptions);

        driver.Navigate().GoToUrl($"https://teams.microsoft.com/v2/?meetingjoin=true#/meet/{meetingId.Replace(" ", "")}?launchAgent=marketing_join&laentry=hero&p={password}&anon=true&deeplinkId=251e9ce4-ef63-44dd-9115-a2d4b9c4f46d");

        // Track the time when participant joins
        var startTime = DateTime.Now;

        // Enter participant name
        while (true)
        {
            var input = driver.FindElements(By.TagName("input")).FirstOrDefault();

            if (input == null)
            {
                Console.WriteLine($"Participant {participantNumber}: Waiting for input element...");
                Thread.Sleep(250);
                continue;
            }

            input.SendKeys($"Test Participant {participantNumber}");
            break;
        }

        // Click the "Join now" button
        while (true)
        {
            var button = driver.FindElements(By.TagName("button"))
                .FirstOrDefault(x => x.Text.Contains("join now", StringComparison.InvariantCultureIgnoreCase));

            if (button == null)
            {
                Console.WriteLine($"Participant {participantNumber}: Waiting for 'Join now' button...");
                Thread.Sleep(250);
                continue;
            }

            button.Click();
            break;
        }

        // Mute microphone if possible
        while (true)
        {
            try
            {
                var button = driver.FindElements(By.TagName("button"))
                    .FirstOrDefault(x => x.GetDomAttribute("id").Contains("microphone-button", StringComparison.InvariantCultureIgnoreCase));

                if (button == null)
                {
                    Console.WriteLine($"Participant {participantNumber}: Waiting for 'Microphone' button...");
                    Thread.Sleep(250);
                    continue;
                }

                button.Click();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Participant {participantNumber}: Exception while clicking microphone button: {ex.Message}");
                Thread.Sleep(250);
            }
        }

        // Wait for 70 minutes (4200 seconds) to timeout
        var timeout = TimeSpan.FromMinutes(70);

        // Check if 70 minutes have passed
        while (running)
        {
            var elapsedTime = DateTime.Now - startTime;
            if (elapsedTime >= timeout)
            {
                Console.WriteLine($"Participant {participantNumber}: Timeout reached after 70 minutes, exiting...");
                break;
            }
            Thread.Sleep(250); // Sleep to reduce CPU load
        }

        // Retry for finding the "hangup" button with a timeout (e.g., 10 seconds)
        IWebElement? hangup = null;
        for (int i = 0; i < 40; i++) // Retry every 250ms for up to 10 seconds
        {
            hangup = driver.FindElements(By.TagName("button"))
                .FirstOrDefault(x => x.GetDomAttribute("id") == "hangup-button");

            if (hangup != null)
            {
                Console.WriteLine($"Participant {participantNumber}: Hangup button found.");
                break;
            }

            Console.WriteLine($"Participant {participantNumber}: Waiting for hangup button...");
            Thread.Sleep(250); // Wait before retrying
        }

        if (hangup == null)
        {
            Console.WriteLine($"Participant {participantNumber}: Hangup button not found after retries.");
        }
        else
        {
            try
            {
                hangup.Click();
                Console.WriteLine($"Participant {participantNumber}: Hangup clicked.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Participant {participantNumber}: Failed to click hangup button: {ex.Message}");
            }
        }

        // Wait a little before closing the browser after timeout or hangup
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
