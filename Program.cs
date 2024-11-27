using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

var threads = new List<Thread>();
var running = true;

// Default values
var meetingId = "938 808 388 348 5";
var password = "YW2eJ9";
var threadCount = 15;

// Check if arguments are provided
if (args.Length == 3)
{
    meetingId = args[0];
    password = args[1];
    if (int.TryParse(args[2], out var parsedCount))
    {
        threadCount = parsedCount;
    }
}

// Launch participants silently (no console output)
for (var i = 0; i < threadCount; i++)
{
    var thread = new Thread((o) =>
    {
        var participantNumber = (int)o;
        var chromeOptions = new ChromeOptions();

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
        var startTime = DateTime.Now;
        var timeout = TimeSpan.FromMinutes(70);

        try
        {
            // Ensure meetingId and password are non-null
            if (string.IsNullOrEmpty(meetingId) || string.IsNullOrEmpty(password))
            {
                return;  // Skip if invalid
            }

            driver.Navigate().GoToUrl($"https://teams.microsoft.com/v2/?meetingjoin=true#/meet/{meetingId.Replace(" ", "")}?launchAgent=marketing_join&laentry=hero&p={password}&anon=true&deeplinkId=251e9ce4-ef63-44dd-9115-a2d4b9c4f46d");

            // Wait for input field to appear
            while (DateTime.Now - startTime < timeout)
            {
                var input = driver.FindElements(By.TagName("input")).FirstOrDefault();
                if (input != null)
                {
                    input.SendKeys($"Test Participant {participantNumber}");
                    break;
                }
                Thread.Sleep(250);
            }

            // Wait for "Join Now" button and click it
            while (DateTime.Now - startTime < timeout)
            {
                var button = driver.FindElements(By.TagName("button"))
                    .FirstOrDefault(x => x.Text.Contains("join now", StringComparison.InvariantCultureIgnoreCase));
                if (button != null)
                {
                    button.Click();
                    break;
                }
                Thread.Sleep(250);
            }

            // Mute microphone if button exists
            while (DateTime.Now - startTime < timeout)
            {
                try
                {
                    var muteButton = driver.FindElements(By.TagName("button"))
                        .FirstOrDefault(x => x.GetDomAttribute("id").Contains("microphone-button", StringComparison.InvariantCultureIgnoreCase));
                    if (muteButton != null)
                    {
                        muteButton.Click();
                        break;
                    }
                }
                catch
                {
                    // Catching any mute-related errors, but not logging
                }
                Thread.Sleep(250);
            }

            // Wait for the participant to stay for the entire duration
            while (DateTime.Now - startTime < timeout && running)
            {
                Thread.Sleep(250);
            }
        }
        catch
        {
            // Silent catch, we don't want any logging
        }
        finally
        {
            try
            {
                var hangup = driver.FindElements(By.TagName("button"))
                    .FirstOrDefault(x => x.GetDomAttribute("id") == "hangup-button");
                if (hangup != null)
                {
                    hangup.Click();
                }
            }
            catch
            {
                // Ignore any errors while attempting to hang up
            }
            finally
            {
                driver?.Quit();  // Ensure the browser closes properly
            }
        }
    });

    threads.Add(thread);
    thread.Start(i + 1);
}

// Let the threads run until the process is manually terminated
for (var i = 0; i < threads.Count; i++)
{
    threads[i].Join(TimeSpan.FromMinutes(70));  // Ensure a timeout for each thread
}
