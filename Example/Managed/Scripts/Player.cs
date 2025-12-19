using GameProject;
using System;

namespace Example.Managed.Scripts
{
    internal class Player : GameScript
    {
        static int s_Counter = 0;
        public Player() { }

        public override void Update()
        {
            Console.WriteLine($"Update {s_Counter++}");
        }
    }
}
