using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace MochiSharp.Managed.Core
{
    public class GameContext : AssemblyLoadContext
    {
        private Assembly _gameAssembly;
        private MethodInfo _updateMethod;
        private object _scriptInstance;

        public GameContext(string pluginPath) : base(isCollectible: true)
        {
            using (var fs = new FileStream(pluginPath, FileMode.Open, FileAccess.Read))
            {
                _gameAssembly = LoadFromStream(fs);
            };

            var type = _gameAssembly.GetType("Example.Managed.Scripts.Player");
            _scriptInstance = Activator.CreateInstance(type!)!;
            _updateMethod = type!.GetMethod("Update")!;
        }

        public void InvokeUpdate()
        {
            _updateMethod?.Invoke(_scriptInstance, null);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
