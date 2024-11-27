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
    threadCount = int.Parse(args[2]);
}

Console.WriteLine("MS Teams Dummy Participant Runner - Using Chrome");
Console.WriteLine("Created by Charan Teja @teja_palleti - https://www.github.com/teja-palleti");

// Display the default values for reference
Console.WriteLine($"Meeting ID: {meetingId}");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Participant count: {threadCount}");

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

            // Wait for the "Join Now" button and click it
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error muting microphone for participant {participantNumber}: {ex.Message}");
                }
                Thread.Sleep(250);
            }

            // If the participant stays for the entire duration, exit gracefully
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
            // Ensure the browser closes properly
            try
            {
                var hangup = driver.FindElements(By.TagName("button"))
                    .FirstOrDefault(x => x.GetDomAttribute("id") == "hangup-button");
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
    threads[i].Join(TimeSpan.FromMinutes(70));  // Ensure a timeout for each thread
}

Console.WriteLine("All threads are finished. Exiting the app.");
