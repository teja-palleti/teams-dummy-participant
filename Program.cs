// Number of participants to join
int threadCount = 10; // Reduced to 10 participants

List<Thread> threads = new List<Thread>();
for (int i = 0; i < threadCount; i++)
{
    var thread = new Thread((o) =>
    {
        int participantNumber = (int)o;
        Console.WriteLine($"Participant {participantNumber} starting...");
        
        // Your existing code to join the meeting
        JoinMeeting(participantNumber);
    });

    threads.Add(thread);
    thread.Start(i + 1); // Starting thread for participant i
    Thread.Sleep(500); // Optional: small delay between threads
}

// Wait for all threads to finish
foreach (var thread in threads)
{
    thread.Join();
}

Console.WriteLine("All participants have joined the meeting.");
