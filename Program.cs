using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

var threads = new List<Thread>();
var running = true;
var meetingId = Environment.GetEnvironmentVariable("MEETING_ID") ?? string.Empty;
var password = Environment.GetEnvironmentVariable("MEETING_PASSWORD") ?? string.Empty;
var threadCount = int.Parse(Environment.GetEnvironmentVariable("PARTICIPANT_COUNT") ?? "10");  // Default to 10 participants

Console.WriteLine("MS Teams Dummy Participant Runner - Using Chrome");

if (string.IsNullOrEmpty(meetingId) || string.IsNullOrEmpty(password))
{
    Console.WriteLine("Please provide the Teams meeting ID and password as environment variables.");
    return;
}

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
            driver.Navigate().GoToUrl($"https://teams.microsoft.com/v2/?meetingjoin=true#/meet/{meetingId.Replace(" ", "")}?launchAgent=marketing_join&laentry=hero&p={password}&anon=true&deeplinkId=251e9ce4-ef63-44dd-9115-a2d4b9c4f46d");

            // Joining process...
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

            // Muting microphone...
            while (DateTime.Now - startTime < timeout)
            {
                try
                {
                    var muteButton = driver.FindElements(By.TagName("button"))
                        .FirstOrDefault(x => x.GetAttribute("id").Contains("microphone-button", StringComparison.InvariantCultureIgnoreCase));
                    if (muteButton != null)
                    {
                        muteButton.Click();
                        break;
                    }
                }
                catch (Exception)
                {
                    // Ignore and retry
                }
                Thread.Sleep(250);
            }

            // Stay for the entire meeting duration
            while (DateTime.Now - startTime < timeout && running)
            {
                Thread.Sleep(250);
            }

            Console.WriteLine($"Participant {participantNumber} is exiting after {timeout.TotalMinutes} minutes.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error for participant {participantNumber}: {ex.Message}");
        }
        finally
        {
            try
            {
                var hangup = driver.FindElements(By.TagName("button"))
                    .FirstOrDefault(x => x.GetAttribute("id") == "hangup-button");
                if (hangup != null)
                {
                    hangup.Click();
                }
                else
                {
                    Console.WriteLine($"Hangup button not found for participant {participantNumber}. Closing the browser.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing participant {participantNumber}: {ex.Message}");
            }
            finally
            {
                driver?.Quit();
            }
        }
    });

    threads.Add(thread);
    thread.Start(i + 1);
}

Console.WriteLine("Launched. Exiting after all threads are finished.");
for (var i = 0; i < threads.Count; i++)
{
    threads[i].Join(TimeSpan.FromMinutes(70));
}

Console.WriteLine("All threads are finished. Exiting the app.");
