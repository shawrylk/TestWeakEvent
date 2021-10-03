using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class DataClass
    {
        public static int StaticID { get; private set; } = 0;
        public int ID { get; init; }
        private int calledCount = 0;
        public string Data { get; set; }

        public DataClass()
        {
            ID = StaticID++;
        }

        ~DataClass()
        {
            Console.WriteLine($"Data class id = {ID} is destroyed");
        }

        public void Method()
        {
            Console.WriteLine($"Data class id = {ID}, called {++calledCount} time(s)");
        }
    }

    public class EventWrapper
    {
        public event Action Events;
        public bool HasEvents => Events?.GetInvocationList().Count() > 0;
        public void Invoke()
        {
            Events?.Invoke();
        }
    }

    class Program
    {
        static async Task Main()
        {
            EventWrapper testEvents = new();

            while (true)
            {
                _ = SpawnTask(testEvents);
                //await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }
        static async Task SpawnTask(EventWrapper testEvents)
        {
            SubscribeToWeakReferenceObject(testEvents);
            while (true)
            {
                if (!testEvents.HasEvents)
                {
                    return;
                }
                else
                {
                    testEvents.Invoke();
                }
                // Delay for GC to kick in
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        static void SubscribeToWeakReferenceObject(EventWrapper testEvents)
        {
            DataClass a = new() { Data = GenerateRandomString() };

            // Right way
            a.ToWeakEvent(
                action: value => value.Method(),
                subscribe: handler => testEvents.Events += handler,
                unsunscribe: handler => testEvents.Events -= handler);

            // Wrong way
            //testEvents.Events += a.Method;
        }

        static Random random = new();
        static string GenerateRandomString(int size = 1_000_000)
        {
            byte[] bytes = new byte[size];
            random.NextBytes(bytes);
            string str = BitConverter.ToString(bytes);
            return str;
        }
    }

    public static class EventEx
    {
        public static void ToWeakEvent<T>(this T @this, Action<T> action, Action<Action> subscribe, Action<Action> unsunscribe)
        {
            var weakReference = new WeakReference(@this);
            Action handler = null;
            handler = () =>
            {
                var obj = (T)weakReference.Target;
                if (obj is null)
                {
                    unsunscribe?.Invoke(handler);
                }
                else
                {
                    action?.Invoke(obj);
                }
            };
            subscribe?.Invoke(handler);
        }
    }
}
