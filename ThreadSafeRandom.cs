using System;
using System.Collections.Generic;
using System.Threading;

namespace reversing_nearness
{
    static class ThreadSafeRandom
    {
        public static int ReproducibleSeed { get; private set; }
        static Random seeder;
        static ThreadLocal<Random> instance;
        public static Random Random => instance.Value;

        static Random GetInstance()
        {
            lock(seeder)
            {
                return new Random(seeder.Next());
            }
        }

        static ThreadSafeRandom()
        {
            ReproducibleSeed = (int)DateTime.UtcNow.Ticks;
            seeder = new Random(ReproducibleSeed);
            instance = new ThreadLocal<Random>(GetInstance);
        }

        public static T ChooseOne<T>(this List<T> source)
        {
            var selectedIndex = Random.Next(source.Count);
            var choice = source[selectedIndex];
            return choice;
        }
    }
}