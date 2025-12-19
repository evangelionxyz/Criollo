using GameProject;
using System;
using Example.Managed.Interop;

namespace Example.Managed.Scripts
{
    internal class Player : GameScript
    {
        private Transform _transform;

        public Player() { }

        public override void OnAwake()
        {
            Console.WriteLine("C# Player On Awake");
        }

        public override void OnStart()
        {
            Console.WriteLine("C# Player On Start");
        }

        public override void OnUpdate(float deltaTime)
        {
            Console.WriteLine($"C# Player On Update dt: {deltaTime}");
        }

        public int AddInt(int a, int b) => a + b;
        public int MulInt(int a, int b) => a * b;

        public Vector3 AddVector(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public Vector3 MulVector(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        public void SetTransform(Transform transform)
        {
            _transform = transform;
            Console.WriteLine($"C# SetTransform: Pos=({_transform.Position.X}, {_transform.Position.Y}, {_transform.Position.Z})");
        }

        public Transform GetTransform() => _transform;
    }
}
